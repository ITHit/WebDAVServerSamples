using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Class2;
using ITHit.WebDAV.Server.ResumableUpload;

namespace WebDAVServer.SqlStorage.HttpListener
{
    /// <summary>
    /// Represents file in WebDAV repository.
    /// </summary>
    public class DavFile : DavHierarchyItem, IFile, IResumableUpload, IUploadProgress
    {

        /// <summary>
        /// Initializes a new instance of the DavFile class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        /// <param name="itemId">Item identifier.</param>
        /// <param name="parentId">Parent identifier.</param>
        /// <param name="name">Item name.</param>
        /// <param name="path">Item path (encoded)</param>
        /// <param name="created">Item creation date.</param>
        /// <param name="modified">Item modification date.</param>
        /// <param name="fileAttributes">File attributes for the item.</param>
        public DavFile(
            DavContext context,
            Guid itemId,
            Guid parentId,
            string name,
            string path,
            DateTime created,
            DateTime modified)
            : base(context, itemId, parentId, name, path, created, modified)
        {
        }

        /// <summary>
        /// Gets item's content type.
        /// </summary>
        public string ContentType
        {
            get
            {
                string itemContentType = null;

                int extIndex = Name.LastIndexOf('.');
                if (extIndex != -1)
                {
                    itemContentType = MimeType.GetMimeType(Name.Substring(extIndex + 1));
                }

                if (string.IsNullOrEmpty(itemContentType))
                {
                    itemContentType = getDbField<string>("ContentType", null);
                }

                if (string.IsNullOrEmpty(itemContentType))
                {
                    itemContentType = "application/octet-stream";
                }

                return itemContentType;
            }
        }

        /// <summary>
        /// Gets file length.
        /// </summary>
        public long ContentLength
        {
            get
            {
                object result = Context.ExecuteScalar<object>(
                    "SELECT DATALENGTH(Content) FROM Item WHERE ItemId = @ItemId",
                    "@ItemId", ItemId);

                return DBNull.Value.Equals(result) ? 0 : Convert.ToInt64(result);
            }
        }

        /// <summary>
        /// Gets Etag.
        /// </summary>
        public string Etag
        {
            get
            {
                int serialNumber = getDbField("SerialNumber", 0);
                return string.Format("{0}-{1}", Modified.ToBinary(), serialNumber);
            }
        }
        /// <summary>
        /// Gets or Sets snippet of file content that matches search conditions.
        /// </summary>
        public string Snippet { get; set; }

        /// <summary>
        /// Reads file's content from storage (to send to client).
        /// </summary>
        /// <param name="output">Stream to read body to.</param>
        /// <param name="byteStart">Number of first byte to write.</param>
        /// <param name="count">Number of bytes to be written.</param>
        public async Task ReadAsync(Stream output, long byteStart, long count)
        {
            if (ContainsDownloadParam(Context.Request.RawUrl))
            {
                AddContentDisposition(Context, Name);
            }
            
            using (SqlDataReader reader = await Context.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess,
                @"SELECT Content FROM Item WHERE ItemId = @ItemId",
                "@ItemId", ItemId))                        
            {
                await reader.ReadAsync();

                long bufSize = 1048576; // 1Mb
                var buf = new byte[bufSize];
                long retval;
                long startIndex = byteStart;
                try
                {
                    while ((retval = reader.GetBytes(
                        reader.GetOrdinal("Content"),
                        startIndex,
                        buf,
                        0,
                        (int)(count > bufSize ? bufSize : count))) > 0)
                    {
                        await output.WriteAsync(buf, 0, (int)retval);
                        startIndex += retval;
                        count -= retval;
                    }
                }
                catch (System.Net.HttpListenerException)
                {
                    // The remote host closed the connection (for example Cancel or Pause pressed).
                }
            }
        }

        internal static bool ContainsDownloadParam(string url)
        {
            int ind = url.IndexOf('?');
            if (ind > 0 && ind < url.Length - 1)
            {
                string[] param = url.Substring(ind + 1).Split('&');
                return param.Any(p => p.StartsWith("download"));
            }
            return false;
        }

        internal static void AddContentDisposition(DavContext context, string name)
        {
            if (context.Request.UserAgent.Contains("MSIE")) // problem with file extensions in IE
            {
                var fileName = EncodeUtil.EncodeUrlPart(name);
                context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + fileName + "\"");
            }
            else
                context.Response.AddHeader("Content-Disposition", "attachment;");
        }

        /// <summary>
        /// Stores file contents to storage (when client updates it).
        /// </summary>
        /// <param name="segment">Stream with new file content.</param>
        /// <param name="contentType">New content type.</param>
        /// <param name="startIndex">Index of first byte in the file where update shall be applied.</param>
        /// <param name="totalContentLength">Length of the file after it will be updated with the new content.</param>
        /// <returns>Boolean value indicating if entire stream was written.</returns>
        public async Task<bool> WriteAsync(Stream segment, string contentType, long startIndex, long totalContentLength)
        {
            await RequireHasTokenAsync();
            await WriteInternalAsync(segment, contentType, startIndex, totalContentLength);
            await Context.socketService.NotifyUpdatedAsync(Path, GetWebSocketID());
            return true;
        }

        /// <summary>
        /// Stores file contents to storage (when client updates it).
        /// </summary>
        /// <param name="segment">Stream with new file content.</param>
        /// <param name="contentType">New content type.</param>
        /// <param name="startIndex">Index of first byte in the file where update shall be applied.</param>
        /// <param name="totalContentLength">Length of the file after it will be updated with the new content.</param>
        /// <returns>Boolean value indicating if entire stream was written.</returns>
        public async Task<bool> WriteInternalAsync(Stream segment, string contentType, long startIndex, long totalContentLength)
        {

            string commandText =
                @"UPDATE Item
                  SET
                      Modified = GETUTCDATE(),
                      TotalContentLength = CASE WHEN @TotalContentLength >= 0 THEN @TotalContentLength ELSE 0 END,
                      ContentType = @ContentType,
                      Content = CASE WHEN @ResetContent = 1 THEN 0x ELSE Content END,
                      SerialNumber = ISNULL(SerialNumber, 0) + 1
                  WHERE ItemId = @ItemId";

            await Context.ExecuteNonQueryAsync(
                 commandText,
                 "@ContentType", (object)contentType ?? DBNull.Value,
                 "@ItemId", ItemId,
                 "@TotalContentLength", totalContentLength,
                 "@ResetContent", startIndex == 0 ? 1 : 0);

            const int bufSize = 1048576; // 1Mb
            byte[] buffer = new byte[bufSize];

            long bytes = 0;

            int lastBytesRead;
            try
            {
                while ((lastBytesRead = await segment.ReadAsync(buffer, 0, bufSize)) > 0)
                {
                    SqlParameter dataParm = new SqlParameter("@Data", SqlDbType.VarBinary, bufSize);
                    SqlParameter offsetParm = new SqlParameter("@Offset", SqlDbType.Int);
                    SqlParameter bytesParm = new SqlParameter("@Bytes", SqlDbType.Int, bufSize);
                    SqlParameter itemIdParm = new SqlParameter("@ItemId", ItemId);

                    dataParm.Value = buffer;
                    dataParm.Size = lastBytesRead;
                    offsetParm.Value = bytes + startIndex;
                    bytesParm.Value = lastBytesRead;

                    string updateItemCommand =
                        @"UPDATE Item SET
                            LastChunkSaved = GetUtcDate(),
                            Content .WRITE(@Data, @Offset, @Bytes)
                          WHERE ItemId = @ItemId";

                    await Context.ExecuteNonQueryAsync(
                        updateItemCommand,
                        dataParm,
                        offsetParm,
                        bytesParm,
                        itemIdParm);

                    bytes += lastBytesRead;
                }
            }
            catch (System.Net.HttpListenerException)
            {
                // The remote host closed the connection (for example Cancel or Pause pressed).
            }
            return true;
        }

        /// <summary>
        /// Copies this file to another folder.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New file name in destination folder.</param>
        /// <param name="deep">Is not used.</param>
        /// <param name="multistatus">Container for errors with items other than this file.</param>
        public override async Task CopyToAsync(
            IItemCollection destFolder,
            string destName,
            bool deep,
            MultistatusException multistatus)
        {
            await CopyToInternalAsync(destFolder, destName, deep, multistatus, 0);
        }

        /// <summary>
        /// Called when this file is being copied.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New file name.</param>
        /// <param name="deep">Whether children items shall be copied. Ignored for files.</param>
        /// <param name="multistatus">Information about items that failed to copy.</param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public override async Task CopyToInternalAsync(
            IItemCollection destFolder, 
            string destName, 
            bool deep, 
            MultistatusException multistatus, 
            int recursionDepth)
        {
            DavFolder destDavFolder = destFolder as DavFolder;
            if (destFolder == null)
            {
                throw new DavException("Destination folder doesn't exist.", DavStatus.CONFLICT);
            }
            if (!await destDavFolder.ClientHasTokenAsync())
            {
                throw new LockedException("Doesn't have token for destination folder.");
            }

            DavHierarchyItem destItem = await destDavFolder.FindChildAsync(destName);
            if (destItem != null)
            {
                try
                {
                    await destItem.DeleteAsync(multistatus);
                }
                catch (DavException ex)
                {
                    multistatus.AddInnerException(destItem.Path, ex);
                    return;
                }
            }

            await CopyThisItemAsync(destDavFolder, null, destName);
            if (recursionDepth == 0)
            {
                await Context.socketService.NotifyCreatedAsync(destFolder.Path + EncodeUtil.EncodeUrlPart(destName), GetWebSocketID());
            }
        }

        /// <summary>
        /// Deletes this file.
        /// </summary>
        /// <param name="multistatus">Is not used.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            await DeleteInternalAsync(multistatus, 0);
        }

        /// <summary>
        /// Called whan this file is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public override async Task DeleteInternalAsync(MultistatusException multistatus, int recursionDepth)
        {
            DavFolder parent = await GetParentAsync();
            if (parent == null)
            {
                throw new DavException("Parent is null.", DavStatus.CONFLICT);
            }
            if (!await parent.ClientHasTokenAsync())
            {
                throw new LockedException();
            }

            if (!await ClientHasTokenAsync())
            {
                throw new LockedException();
            }

            await DeleteThisItemAsync(parent);
            if (recursionDepth == 0)
            {
                await Context.socketService.NotifyDeletedAsync(Path, GetWebSocketID());
            }
        }

        /// <summary>
        /// Moves this file to different folder and renames it.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New file name.</param>
        /// <param name="multistatus">Container for errors with items other than this file.</param>
        public override async Task MoveToAsync(IItemCollection destFolder, string destName, MultistatusException multistatus)
        {
            await MoveToInternalAsync(destFolder, destName, multistatus, 0);
        }

        /// <summary>
        /// Called when this file is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this file.</param>
        /// <param name="multistatus">Information about items that failed to move.</param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public override async Task MoveToInternalAsync(IItemCollection destFolder, string destName, MultistatusException multistatus, int recursionDepth)
        {
            DavFolder destDavFolder = destFolder as DavFolder;
            if (destFolder == null)
            {
                throw new DavException("Destination folder doesn't exist.", DavStatus.CONFLICT);
            }

            DavFolder parent = await GetParentAsync();
            if (parent == null)
            {
                throw new DavException("Cannot move root.", DavStatus.CONFLICT);
            }
            if (!await ClientHasTokenAsync() || !await destDavFolder.ClientHasTokenAsync() || !await parent.ClientHasTokenAsync())
            {
                throw new LockedException();
            }

            DavHierarchyItem destItem = await destDavFolder.FindChildAsync(destName);
            if (destItem != null)
            {
                try
                {
                    await destItem.DeleteAsync(multistatus);
                }
                catch (DavException ex)
                {
                    multistatus.AddInnerException(destItem.Path, ex);
                    return;
                }
            }

            await MoveThisItemAsync(destDavFolder, destName, parent);
            // Refresh client UI.
            if (recursionDepth == 0)
            {
                await Context.socketService.NotifyMovedAsync(Path, destDavFolder.Path, GetWebSocketID());
            }
        }
        /// <summary>
        /// Cancels incomplete upload.
        /// </summary>
        public async Task CancelUploadAsync()
        {
            await DeleteAsync(null);
        }

        /// <summary>
        /// Gets time when last upload piece was saved to disk.
        /// </summary>
        public DateTime LastChunkSaved
        {
            get { return getDbField("LastChunkSaved", DateTime.MinValue); }
        }

        /// <summary>
        /// Gets bytes uploaded sofar.
        /// </summary>
        public long BytesUploaded
        {
            get { return ContentLength; }
        }

        /// <summary>
        /// Gets length of the file which it will have after upload finishes.
        /// </summary>
        public long TotalContentLength
        {
            get { return getDbField<long>("TotalContentLength", -1); }
        }

        /// <summary>
        /// Returns instance of <see cref="IResumableUpload"/> interface for this item.
        /// </summary>
        /// <returns>Instance of <see cref="IResumableUpload"/> interface.</returns>
        public async Task<IEnumerable<IResumableUpload>> GetUploadProgressAsync()
        {
            return new[] { this };
        }

        /// <summary>
        /// Returns database field of the file.
        /// </summary>
        /// <typeparam name="T">Type of field value.</typeparam>
        /// <param name="columnName">DB columen in which field is stored.</param>
        /// <param name="defaultValue">Default value to return.</param>
        /// <returns>Value from database or default value if it is null.</returns>
        private T getDbField<T>(string columnName, T defaultValue)
        {
            string commandText = string.Format("SELECT {0} FROM Item WHERE ItemId = @ItemId", columnName);
            object obj = Context.ExecuteScalar<object>(commandText, "@ItemId", ItemId);

            return obj != null ? (T)obj : defaultValue;
        }
    }
}
