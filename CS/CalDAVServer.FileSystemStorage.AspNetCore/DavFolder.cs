
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Class1;
using CalDAVServer.FileSystemStorage.AspNetCore.Acl;
using CalDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;
using ITHit.WebDAV.Server.Search;
using ITHit.WebDAV.Server.Paging;
using System.Data.OleDb;

namespace CalDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Folder in WebDAV repository.
    /// </summary>
    public class DavFolder : DavHierarchyItem, IFolder
    {

        // Control characters and permanently undefined Unicode characters to be removed from search snippet.
        private static readonly Regex invalidXmlCharsPattern = new Regex(@"[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]", RegexOptions.IgnoreCase);

        /// <summary>
        /// Corresponding instance of <see cref="DirectoryInfo"/>.
        /// </summary>
        private readonly DirectoryInfo dirInfo;

        /// <summary>
        /// Returns folder that corresponds to path.
        /// </summary>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        /// <returns>Folder instance or null if physical folder not found in file system.</returns>
        public static async Task<DavFolder> GetFolderAsync(DavContext context, string path)
        {
            string folderPath = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            DirectoryInfo folder = new DirectoryInfo(folderPath);

            // This code blocks vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'.
            if (!folder.Exists || string.Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return null;
            }

            return new DavFolder(folder, context, path);
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="directory">Corresponding folder in the file system.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        protected DavFolder(DirectoryInfo directory, DavContext context, string path)
            : base(directory, context, path.TrimEnd('/') + "/")
        {
            dirInfo = directory;
        }   

        /// <summary>
        /// Called when children of this folder with paging information are being listed.
        /// </summary>
        /// <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Items requested by the client and a total number of items in this folder.</returns>
        public virtual async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            // Enumerates all child files and folders.
            // You can filter children items in this implementation and 
            // return only items that you want to be visible for this 
            // particular user.

            IList<IHierarchyItem> children = new List<IHierarchyItem>();

            long totalItems = 0;
            FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos();
            totalItems = fileInfos.Length;

            // Apply sorting.
            fileInfos = SortChildren(fileInfos, orderProps);

            // Apply paging.
            if (offset.HasValue && nResults.HasValue)
            {
                fileInfos = fileInfos.Skip((int)offset.Value).Take((int)nResults.Value).ToArray();
            }

            foreach (FileSystemInfo fileInfo in fileInfos)
            {
                string childPath = Path + EncodeUtil.EncodeUrlPart(fileInfo.Name);
                IHierarchyItem child = await context.GetHierarchyItemAsync(childPath);
                if (child != null)
                {
                    children.Add(child);
                }
            }

            return new PageResults(children, totalItems);
        }

        /// <summary>
        /// Called when a new file is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new file.</param>
        /// <param name="content">Stream to read the content of the file from.</param>
        /// <param name="contentType">Indicates the media type of the file.</param>
        /// <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        /// <returns>The new file.</returns>
        public virtual async Task<IFile> CreateFileAsync(string name, Stream content, string contentType, long totalFileSize)
        {
            string fileName = System.IO.Path.Combine(fileSystemInfo.FullName, name);

            await using (FileStream stream = new FileStream(fileName, FileMode.CreateNew))
            {
            }

            DavFile file = (DavFile)await context.GetHierarchyItemAsync(Path + EncodeUtil.EncodeUrlPart(name));
            // write file content
            await file.WriteInternalAsync(content, contentType, 0, totalFileSize);

            return file;
        }

        /// <summary>
        /// Called when a new folder is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new folder.</param>
        virtual public async Task CreateFolderAsync(string name)
        {
            await CreateFolderInternalAsync(name);
        }

        /// <summary>
        /// Called when a new folder is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new folder.</param>
        private async Task CreateFolderInternalAsync(string name)
        {

            bool isRoot = dirInfo.Parent == null;
            DirectoryInfo di = isRoot ? new DirectoryInfo(@"\\?\" + context.RepositoryPath.TrimEnd(System.IO.Path.DirectorySeparatorChar)) : dirInfo;
            di.CreateSubdirectory(name);
        }

        /// <summary>
        /// Called when this folder is being copied.
        /// </summary>
        /// <param name="destFolder">Destination parent folder.</param>
        /// <param name="destName">New folder name.</param>
        /// <param name="deep">Whether children items shall be copied.</param>
        /// <param name="multistatus">Information about child items that failed to copy.</param>
        public override async Task CopyToAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            await CopyToInternalAsync(destFolder, destName, deep, multistatus, 0);
        }

        /// <summary>
        /// Called when this folder is being copied.
        /// </summary>
        /// <param name="destFolder">Destination parent folder.</param>
        /// <param name="destName">New folder name.</param>
        /// <param name="deep">Whether children items shall be copied.</param>
        /// <param name="multistatus">Information about child items that failed to copy.</param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public override async Task CopyToInternalAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus, int recursionDepth)
        {
            if (!(destFolder is DavFolder))
            {
                throw new DavException("Target folder doesn't exist", DavStatus.CONFLICT);
            }

            DavFolder targetFolder = (DavFolder)destFolder;

            if (IsRecursive(targetFolder))
            {
                throw new DavException("Cannot copy to subfolder", DavStatus.FORBIDDEN);
            }

            string newDirLocalPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);

            // Create folder at the destination.
            try
            {
                if (!Directory.Exists(newDirLocalPath))
                {
                    await targetFolder.CreateFolderInternalAsync(destName);
                }
            }
            catch (DavException ex)
            {
                // Continue, but report error to client for the target item.
                multistatus.AddInnerException(targetPath, ex);
            }

            // Copy children.
            IFolder createdFolder = (IFolder)await context.GetHierarchyItemAsync(targetPath);
            foreach (DavHierarchyItem item in (await GetChildrenAsync(new PropertyName[0], null, null, new List<OrderProperty>())).Page)
            {
                if (!deep && item is DavFolder)
                {
                    continue;
                }

                try
                {
                    await item.CopyToInternalAsync(createdFolder, item.Name, deep, multistatus, recursionDepth + 1);
                }
                catch (DavException ex)
                {
                    // If a child item failed to copy we continue but report error to client.
                    multistatus.AddInnerException(item.Path, ex);
                }
            }
        }

        /// <summary>
        /// Called when this folder is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="multistatus">Information about child items that failed to move.</param>
        public override async Task MoveToAsync(IItemCollection destFolder, string destName, MultistatusException multistatus)
        {
            await MoveToInternalAsync(destFolder, destName, multistatus, 0);
        }


        /// <summary>
        /// Called when this folder is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="multistatus">Information about child items that failed to move.</param>
        public override async Task MoveToInternalAsync(IItemCollection destFolder, string destName, MultistatusException multistatus, int recursionDepth)
        {
            // in this function we move item by item, because we want to check if each item is not locked.
            if (!(destFolder is DavFolder))
            {
                throw new DavException("Target folder doesn't exist", DavStatus.CONFLICT);
            }

            DavFolder targetFolder = (DavFolder)destFolder;

            if (IsRecursive(targetFolder))
            {
                throw new DavException("Cannot move folder to its subtree.", DavStatus.FORBIDDEN);
            }

            string newDirPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);

            try
            {
                // Remove item with the same name at destination if it exists.
                DavHierarchyItem item = (await context.GetHierarchyItemAsync(targetPath) as DavHierarchyItem);
                if (item != null)
                    await item.DeleteInternalAsync(multistatus, recursionDepth + 1);

                await targetFolder.CreateFolderInternalAsync(destName);
            }
            catch (DavException ex)
            {
                // Continue the operation but report error with destination path to client.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }

            // Move child items.
            bool movedSuccessfully = true;
            IFolder createdFolder = (IFolder)await context.GetHierarchyItemAsync(targetPath);
            foreach (DavHierarchyItem item in (await GetChildrenAsync(new PropertyName[0], null, null, new List<OrderProperty>())).Page)
            {
                try
                {
                    await item.MoveToInternalAsync(createdFolder, item.Name, multistatus, recursionDepth + 1);
                }
                catch (DavException ex)
                {
                    // Continue the operation but report error with child item to client.
                    multistatus.AddInnerException(item.Path, ex);
                    movedSuccessfully = false;
                }
            }

            if (movedSuccessfully)
            {
                await DeleteInternalAsync(multistatus, recursionDepth + 1);
            }
        }

        /// <summary>
        /// Called whan this folder is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            await DeleteInternalAsync(multistatus, 0);
        }

        /// <summary>
        /// Called whan this folder is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public override async Task DeleteInternalAsync(MultistatusException multistatus, int recursionDepth)
        {
            /*
            if (await GetParentAsync() == null)
            {
                throw new DavException("Cannot delete root.", DavStatus.NOT_ALLOWED);
            }
            */
            bool allChildrenDeleted = true;
            foreach (DavHierarchyItem child in (await GetChildrenAsync(new PropertyName[0], null, null, new List<OrderProperty>())).Page)
            {
                try
                {
                    await child.DeleteInternalAsync(multistatus, recursionDepth + 1);
                }
                catch (DavException ex)
                {
                    //continue the operation if a child failed to delete. Tell client about it by adding to multistatus.
                    multistatus.AddInnerException(child.Path, ex);
                    allChildrenDeleted = false;
                }
            }

            if (allChildrenDeleted)
            {
                dirInfo.Delete();
            }
        }

        /// <summary>
        /// Determines whether <paramref name="destFolder"/> is inside this folder.
        /// </summary>
        /// <param name="destFolder">Folder to check.</param>
        /// <returns>Returns <c>true</c> if <paramref name="destFolder"/> is inside this folder.</returns>
        private bool IsRecursive(DavFolder destFolder)
        {
            return destFolder.Path.StartsWith(Path);
        }

        /// <summary>
        /// Sorts array of FileSystemInfo according to the specified order.
        /// </summary>
        /// <param name="fileInfos">Array of files and folders to sort.</param>
        /// <param name="orderProps">Sorting order.</param>
        /// <returns>Sorted list of files and folders.</returns>
        private FileSystemInfo[] SortChildren(FileSystemInfo[] fileInfos, IList<OrderProperty> orderProps)
        {
            if (orderProps != null && orderProps.Count() != 0)
            {
                // map DAV properties to FileSystemInfo 
                Dictionary<string, string> mappedProperties = new Dictionary<string, string>()
                { { "displayname", "Name" }, { "getlastmodified", "LastWriteTime" }, { "getcontenttype", "Extension" },
                  { "quota-used-bytes", "ContentLength" }, { "is-directory", "IsDirectory" } };
                if (orderProps.Count != 0)
                {
                    IOrderedEnumerable<FileSystemInfo> orderedFileInfos = fileInfos.OrderBy(p => p.Name); // init sorting by item Name
                    int index = 0;

                    foreach (OrderProperty ordProp in orderProps)
                    {
                        string propertyName = mappedProperties[ordProp.Property.Name];
                        Func<FileSystemInfo, object> sortFunc = p => p.Name; // default sorting by item Name
                        PropertyInfo propertyInfo = (typeof(FileSystemInfo)).GetProperties().FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

                        if (propertyInfo != null)
                        {
                            sortFunc = p => p.GetType().GetProperty(propertyInfo.Name).GetValue(p);
                        }
                        else if (propertyName == "IsDirectory")
                        {
                            sortFunc = p => p.IsDirectory();
                        }
                        else if (propertyName == "ContentLength")
                        {
                            sortFunc = p => p is FileInfo ? ((FileInfo)p).Length : 0;
                        }

                        if (index++ == 0)
                        {
                            if (ordProp.Ascending)
                                orderedFileInfos = fileInfos.OrderBy(sortFunc);
                            else
                                orderedFileInfos = fileInfos.OrderByDescending(sortFunc);
                        }
                        else
                        {
                            if (ordProp.Ascending)
                                orderedFileInfos = orderedFileInfos.ThenBy(sortFunc);
                            else
                                orderedFileInfos = orderedFileInfos.ThenByDescending(sortFunc);
                        }
                    }

                    fileInfos = orderedFileInfos.ToArray();
                }
            }

            return fileInfos;
        }
    }
}
