using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.ResumableUpload;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// Represents file in WebDAV repository.
    /// </summary>
    public class DavFile : DavHierarchyItem, IFileAsync, IResumableUploadAsync, IUploadProgressAsync
    {
        /// <summary>
        /// Corresponding <see cref="DataLakeItem"/>.
        /// </summary>
        private readonly DataLakeItem dataLakeItem;
        /// <summary>
        /// Value updated every time this file is updated. Used to form Etag.
        /// </summary>
        private int serialNumber;
        /// <summary>
        /// Gets content type.
        /// </summary>
        public string ContentType => dataLakeItem.ContentType;
        /// <summary>
        /// Gets length of the file.
        /// </summary>
        public long ContentLength => dataLakeItem.ContentLength;

        /// <summary>
        /// Gets entity tag - string that identifies current state of resource's content.
        /// </summary>
        /// <remarks>This property shall return different value if content changes.</remarks>
        public string Etag => $"{Modified.ToBinary()}-{serialNumber}";

        /// <summary>
        /// Returns file that corresponds to path.
        /// </summary>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        /// <returns>File instance or null if physical file is not found in file system.</returns>
        public static async Task<DavFile> GetFileAsync(DavContext context, string path)
        {
            DataLakeItem dlItem = await context.DataLakeStoreService.GetItemAsync(path);
            DavFile davFile = new DavFile(dlItem, context, path)
            {
                serialNumber = dlItem.Properties.TryGetValue("SerialNumber", out var sNumber) ? int.Parse(sNumber) : 0,
                TotalContentLength = dlItem.Properties.TryGetValue("TotalContentLength", out var value)
                    ? long.Parse(value)
                    : 0
            };

            return davFile;
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="dataLakeItem">Corresponding data lake item.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        internal DavFile(DataLakeItem dataLakeItem, DavContext context, string path)
            : base(dataLakeItem, context, path)
        {
            this.dataLakeItem = dataLakeItem;
        }

        /// <summary>
        /// Called when a client is downloading a file. Copies file contents to ouput stream.
        /// </summary>
        /// <param name="output">Stream to copy contents to.</param>
        /// <param name="startIndex">The zero-bazed byte offset in file content at which to begin copying bytes to the output stream.</param>
        /// <param name="count">The number of bytes to be written to the output stream.</param>
        public virtual async Task ReadAsync(Stream output, long startIndex, long count)
        {
            if (ContainsDownloadParam(context.Request.RawUrl))
            {
                AddContentDisposition(Name);
            }
            try
            {
                await context.DataLakeStoreService.ReadItemAsync(Path, output, startIndex, count);
            }
            catch (RequestFailedException ex)
            {
                // The remote host closed the connection (for example Cancel or Pause pressed).
                context.Logger.LogError(ex.Message, ex);
            }
        }

        /// <summary>
        /// Called when a file or its part is being uploaded.
        /// </summary>
        /// <param name="content">Stream to read the content of the file from.</param>
        /// <param name="contentType">Indicates the media type of the file.</param>
        /// <param name="startIndex">Starting byte in target file
        /// for which data comes in <paramref name="content"/> stream.</param>
        /// <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        /// <returns>Whether the whole stream has been written. This result is used by the engine to determine
        /// if auto check-in shall be performed (if auto versioning is used).</returns>
        public virtual async Task<bool> WriteAsync(Stream content, string contentType, long startIndex, long totalFileSize)
        {
            await RequireHasTokenAsync();
            await context.DataLakeStoreService.WriteItemAsync(Path, content, totalFileSize, dataLakeItem.Properties);
            await UpdateLastModified(DateTime.UtcNow);
            await context.DataLakeStoreService.SetExtendedAttributeAsync(dataLakeItem, "TotalContentLength", totalFileSize);
            await context.DataLakeStoreService.SetExtendedAttributeAsync(dataLakeItem, "SerialNumber", ++serialNumber);
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));
            return true;
        }

        /// <summary>
        /// Called when this file is being copied.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New file name.</param>
        /// <param name="deep">Whether children items shall be copied. Ignored for files.</param>
        /// <param name="multistatus">Information about items that failed to copy.</param>
        public override async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            DavFolder targetFolder = (DavFolder)destFolder;
            var existenceResult = await context.DataLakeStoreService.ExistsAsync(targetFolder.Path);
            if (!existenceResult.Exists)
            {
                throw new DavException("Target directory doesn't exist", DavStatus.CONFLICT);
            }

            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);
            // If an item with the same name exists - remove it.
            try
            {
                if (await context.GetHierarchyItemAsync(targetPath) is { } item)
                {
                    await item.DeleteAsync(multistatus);
                }
            }
            catch (DavException ex)
            {
                // Report exception to client and continue with other items by returning from recursion.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }

            await context.DataLakeStoreService.CopyItemAsync(Path, targetFolder.Path, destName, ContentLength, dataLakeItem.Properties);
            await context.socketService.NotifyRefreshAsync(targetFolder.Path);
        }

        /// <summary>
        /// Called when this file is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this file.</param>
        /// <param name="multistatus">Information about items that failed to move.</param>
        public override async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            await RequireHasTokenAsync();

            DavFolder targetFolder = (DavFolder)destFolder;

            var existenceResult = await context.DataLakeStoreService.ExistsAsync(targetFolder.Path);
            if (!existenceResult.Exists)
            {
                throw new DavException("Target directory doesn't exist", DavStatus.CONFLICT);
            }
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);
            
            // If an item with the same name exists in target directory - remove it.
            try
            {
                if (await context.GetHierarchyItemAsync(targetPath) is { } item)
                {
                    await item.DeleteAsync(multistatus);
                }
            }
            catch (DavException ex)
            {
                // Report exception to client and continue with other items by returning from recursion.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }

            await context.DataLakeStoreService.CopyItemAsync(Path, targetFolder.Path, destName, ContentLength, dataLakeItem.Properties);
            await DeleteAsync(multistatus);
            // Refresh client UI.
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));
            await context.socketService.NotifyRefreshAsync(targetFolder.Path);
        }

        /// <summary>
        /// Called when this file is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            await RequireHasTokenAsync();
            try
            {
                await context.DataLakeStoreService.DeleteItemAsync(Path);
            }
            catch (RequestFailedException ex)
            {
                throw new DataException($"Cannot delete item {Name}", ex);
            }
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));
        }

        /// <summary>
        /// Called when client cancels upload in Ajax client.
        /// </summary>
        /// <remarks>
        /// Client do not plan to restore upload. Remove any temporary files / cleanup resources here.
        /// </remarks>
        public async Task CancelUploadAsync()
        {
            await DeleteAsync(new MultistatusException());
        }

        /// <summary>
        /// Gets date when last chunk was saved to this file.
        /// </summary>
        public DateTime LastChunkSaved => dataLakeItem?.ModifiedUtc ?? DateTime.MinValue;

        /// <summary>
        /// Gets number of bytes uploaded so far.
        /// </summary>
        public long BytesUploaded => ContentLength;

        /// <summary>
        /// Gets total length of the file being uploaded.
        /// </summary>
        public long TotalContentLength
        {
            get; private set;
        }

        /// <summary>
        /// Returns instance of <see cref="IUploadProgressAsync"/> interface.
        /// </summary>
        /// <returns>Just returns this class.</returns>
        public async Task<IEnumerable<IResumableUploadAsync>> GetUploadProgressAsync()
        {
            return await Task.Run(() => new[] { this });
        }

        private static bool ContainsDownloadParam(string url)
        {
            int ind = url.IndexOf('?');
            if (ind > 0 && ind < url.Length - 1)
            {
                string[] param = url.Substring(ind + 1).Split('&');
                return param.Any(p => p.StartsWith("download"));
            }
            return false;
        }

        /// <summary>
        /// Adds Content-Disposition header.
        /// </summary>
        /// <param name="name">File name to specified in Content-Disposition header.</param>
        private void AddContentDisposition(string name)
        {
            // Content-Disposition header must be generated differently in case if IE and other web browsers.
            if (context.Request.UserAgent.Contains("MSIE"))
            {
                string fileName = EncodeUtil.EncodeUrlPart(name);
                string attachment = string.Format("attachment filename=\"{0}\"", fileName);
                context.Response.AddHeader("Content-Disposition", attachment);
            }
            else
            {
                context.Response.AddHeader("Content-Disposition", "attachment");
            }
        }
    }
}
