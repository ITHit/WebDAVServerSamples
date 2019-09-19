using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Class1;
using CalDAVServer.FileSystemStorage.AspNet.ExtendedAttributes;

namespace CalDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Represents file in WebDAV repository.
    /// </summary>
    public class DavFile : DavHierarchyItem, IFileAsync
    {
        /// <summary>
        /// Corresponding <see cref="FileInfo"/>.
        /// </summary>
        private readonly FileInfo fileInfo;

        /// <summary>
        /// Size of chunks to upload/download.
        /// (1Mb) buffer size used when reading and writing file content.
        /// </summary>
        private const int bufSize = 1048576;

        /// <summary>
        /// Value updated every time this file is updated. Used to form Etag.
        /// </summary>
        private int serialNumber;

        /// <summary>
        /// Gets content type.
        /// </summary>
        public string ContentType
        {
            get { return MimeType.GetMimeType(fileSystemInfo.Extension) ?? "application/octet-stream"; }
        }

        /// <summary>
        /// Gets length of the file.
        /// </summary>
        public long ContentLength
        {
            get { return fileInfo.Length; }
        }

        /// <summary>
        /// Gets entity tag - string that identifies current state of resource's content.
        /// </summary>
        /// <remarks>This property shall return different value if content changes.</remarks>
        public string Etag
        {
            get { return string.Format("{0}-{1}", Modified.ToBinary(), this.serialNumber); }
        }

        /// <summary>
        /// Returns file that corresponds to path.
        /// </summary>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        /// <returns>File instance or null if physical file is not found in file system.</returns>
        public static async Task<DavFile> GetFileAsync(DavContext context, string path)
        {
            string filePath = context.MapPath(path);
            FileInfo file = new FileInfo(filePath);

            // This code blocks vulnerability when "%20" folder can be injected into path and file.Exists returns 'true'.
            if (!file.Exists || string.Compare(file.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), filePath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return null;
            }

            DavFile davFile = new DavFile(file, context, path);

            if (await file.HasExtendedAttributeAsync("SerialNumber"))
            {
                davFile.serialNumber = await file.GetExtendedAttributeAsync<int?>("SerialNumber") ?? 0;
            }

            return davFile;
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="file">Corresponding file in the file system.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        protected DavFile(FileInfo file, DavContext context, string path)
            : base(file, context, path)
        {
            this.fileInfo = file;
        }

        /// <summary>
        /// Called when a client is downloading a file. Copies file contents to ouput stream.
        /// </summary>
        /// <param name="output">Stream to copy contents to.</param>
        /// <param name="startIndex">The zero-bazed byte offset in file content at which to begin copying bytes to the output stream.</param>
        /// <param name="count">The number of bytes to be written to the output stream.</param>
        public virtual async Task ReadAsync(Stream output, long startIndex, long count)
        {
            //Set timeout to maximum value to be able to download large files.
            HttpContext.Current.Server.ScriptTimeout = int.MaxValue;
            if (ContainsDownloadParam(context.Request.RawUrl))
            {
                AddContentDisposition(Name);
            }

            byte[] buffer = new byte[bufSize];
            using (FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.Seek(startIndex, SeekOrigin.Begin);
                int bytesRead;
                int toRead = (int)Math.Min(count, bufSize);
                if (toRead <= 0)
                {
                    return;
                }

                try
                {
                    bytesRead = await fileStream.ReadAsync(buffer, 0, toRead);
                    while (bytesRead  > 0)
                    {
                        await output.WriteAsync(buffer, 0, bytesRead);
                        count -= bytesRead;
                        bytesRead = await fileStream.ReadAsync(buffer, 0, toRead);
                    }
                }
                catch (HttpException)
                {
                    // The remote host closed the connection (for example Cancel or Pause pressed).
                }
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
        /// if auto checkin shall be performed (if auto versioning is used).</returns>
        public virtual async Task<bool> WriteAsync(Stream content, string contentType, long startIndex, long totalFileSize)
        {
            //Set timeout to maximum value to be able to upload large files.
            HttpContext.Current.Server.ScriptTimeout = int.MaxValue;
            if (startIndex == 0 && fileInfo.Length > 0)
            {
                using (FileStream filestream = fileInfo.Open(FileMode.Truncate)) { }
            }
            await fileInfo.SetExtendedAttributeAsync("SerialNumber", ++this.serialNumber);

            using (FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
            {
                if (fileStream.Length < startIndex)
                {
                    throw new DavException("Previous piece of file was not uploaded.", DavStatus.PRECONDITION_FAILED);
                }

                if (!content.CanRead)
                {
                    return true;
                }

                fileStream.Seek(startIndex, SeekOrigin.Begin);
                byte[] buffer = new byte[bufSize];

                int lastBytesRead;
                try
                {
                    lastBytesRead = await content.ReadAsync(buffer, 0, bufSize);
                    while (lastBytesRead > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, lastBytesRead);
                        await fileStream.FlushAsync();
                        lastBytesRead = await content.ReadAsync(buffer, 0, bufSize);
                    }
                }
                catch (HttpException)
                {
                    // The remote host closed the connection (for example Cancel or Pause pressed).
                }
            }
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

            if (targetFolder == null || !Directory.Exists(targetFolder.FullPath))
            {
                throw new DavException("Target directory doesn't exist", DavStatus.CONFLICT);
            }

            string newFilePath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);

            // If an item with the same name exists - remove it.
            try
            {
                IHierarchyItemAsync item = await context.GetHierarchyItemAsync(targetPath);
                if (item != null)
                    await item.DeleteAsync(multistatus);
            }
            catch (DavException ex)
            {
                // Report error with other item to client.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }

            // Copy the file togather with all extended attributes (custom props and locks).
            try
            {
                File.Copy(fileSystemInfo.FullName, newFilePath);

                var newFileSystemInfo = new FileInfo(newFilePath);
                if (FileSystemInfoExtension.IsUsingFileSystemAttribute)
                {
                    await fileSystemInfo.CopyExtendedAttributes(newFileSystemInfo);
                }

                // Locks should not be copied, delete them.
                if (await fileSystemInfo.HasExtendedAttributeAsync("Locks"))
                {
                    await newFileSystemInfo.DeleteExtendedAttributeAsync("Locks");
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Fail
                NeedPrivilegesException ex = new NeedPrivilegesException("Not enough privileges");
                string parentPath = System.IO.Path.GetDirectoryName(Path);
                ex.AddRequiredPrivilege(parentPath, Privilege.Bind);
                throw ex;
            }
        }

        /// <summary>
        /// Called when this file is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this file.</param>
        /// <param name="multistatus">Information about items that failed to move.</param>
        public override async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {

            DavFolder targetFolder = (DavFolder)destFolder;

            if (targetFolder == null || !Directory.Exists(targetFolder.FullPath))
            {
                throw new DavException("Target directory doesn't exist", DavStatus.CONFLICT);
            }

            string newDirPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);

            // If an item with the same name exists in target directory - remove it.
            try
            {
                IHierarchyItemAsync item = await context.GetHierarchyItemAsync(targetPath) as IHierarchyItemAsync;

                if (item != null)
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

            // Move the file.
            try
            {
                File.Move(fileSystemInfo.FullName, newDirPath);

                var newFileInfo = new FileInfo(newDirPath);
                if (FileSystemInfoExtension.IsUsingFileSystemAttribute)
                {
                    await fileSystemInfo.MoveExtendedAttributes(newFileInfo);
                }

                // Locks should not be copied, delete them.
                if (await newFileInfo.HasExtendedAttributeAsync("Locks"))
                    await newFileInfo.DeleteExtendedAttributeAsync("Locks");
            }
            catch (UnauthorizedAccessException)
            {
                // Exception occurred with the item for which MoveTo was called - fail the operation.
                NeedPrivilegesException ex = new NeedPrivilegesException("Not enough privileges");
                ex.AddRequiredPrivilege(targetPath, Privilege.Bind);

                string parentPath = System.IO.Path.GetDirectoryName(Path);
                ex.AddRequiredPrivilege(parentPath, Privilege.Unbind);
                throw ex;
            }
        }

        /// <summary>
        /// Called whan this file is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            if (FileSystemInfoExtension.IsUsingFileSystemAttribute)
            {
                await fileSystemInfo.DeleteExtendedAttributes();
            }

            fileSystemInfo.Delete();
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
