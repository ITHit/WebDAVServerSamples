
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.ResumableUpload;
using ITHit.WebDAV.Server.Paging;
using Azure.Storage.Files.DataLake;
using Azure.Storage.Files.DataLake.Models;

namespace AzureDataLakeStorage
{
    /// <summary>
    /// Folder in WebDAV repository.
    /// </summary>
    public class DavFolder : DavHierarchyItem, IFolderAsync, IResumableUploadBase
    {

        /// <summary>
        /// Corresponding instance of <see cref="DataLakeDirectoryClient"/>.
        /// </summary>
        private readonly DataLakeDirectoryClient dataLakeDirectoryClient;

        /// <summary>
        /// Returns folder that corresponds to path.
        /// </summary>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        /// <returns>Folder instance or null if physical folder not found in file system.</returns>
        public static async Task<DavFolder> GetFolderAsync(DavContext context, string path)
        {
            DataLakeDirectoryClient dataLakeDirectoryClient = await context.GetDirectoryClient(path);
            if (dataLakeDirectoryClient == null) return null;
            var properties = await dataLakeDirectoryClient.GetPropertiesAsync();
            var dlItem = new DLItem
            {
                ContentLength = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                Name = EncodeUtil.DecodeUrlPart(dataLakeDirectoryClient.Name),
                Path = dataLakeDirectoryClient.Path,
                CreatedUtc = properties.Value.CreatedOn.UtcDateTime,
                ModifiedUtc = properties.Value.LastModified.UtcDateTime,
                Properties = properties.Value.Metadata
            };
            return new DavFolder(dataLakeDirectoryClient, dlItem, context, path);
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="dataLakeDirectoryClient">Corresponding data lake folder client.</param>
        /// <param name="dlItem">Corresponding DLItem.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        protected DavFolder(DataLakeDirectoryClient dataLakeDirectoryClient, DLItem dlItem, DavContext context, string path)
            : base(dataLakeDirectoryClient, dlItem, context, path.TrimEnd('/') + "/")
        {
            this.dataLakeDirectoryClient = dataLakeDirectoryClient;
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
            IList<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();
            var childData = await GetChildrenAsync(dataLakeDirectoryClient);
            long totalItems = childData.Count;
            // Apply sorting.
            childData = SortChildren(childData, orderProps);
            // Apply paging.
            if (offset.HasValue && nResults.HasValue)
            {
                childData = childData.Skip((int)offset.Value).Take((int)nResults.Value).ToArray();
            }
            foreach (PathItem pathItem in childData)
            {
                IHierarchyItemAsync child;
                var path = pathItem.Name;
                var realName = path;
                if (path.Contains("/"))
                {
                    realName = path.Substring(path.LastIndexOf("/", StringComparison.Ordinal) + 1);
                }
                var dlItem = new DLItem
                {
                    ContentLength = pathItem.ContentLength ?? 0,
                    // ContentType = pathItem.,
                    Name = realName,
                    Path = EncodePath(pathItem.Name),
                    CreatedUtc = pathItem.LastModified.UtcDateTime,
                    ModifiedUtc = pathItem.LastModified.UtcDateTime,
                };
                if (pathItem.IsDirectory.HasValue && pathItem.IsDirectory.Value)
                {
                    child = new DavFolder(await context.GetDirectoryClient(path, true), dlItem, context, dlItem.Path);
                }
                else
                {
                    var dataLakeFileClient = await context.GetFileClient(path, true);
                    var propertiesAsync = await dataLakeFileClient.GetPropertiesAsync();
                    dlItem.Properties =  propertiesAsync.Value.Metadata;
                    dlItem.ContentType =  propertiesAsync.Value.ContentType;
                    child = new DavFile(dataLakeFileClient, dlItem, context, dlItem.Path);
                }
                children.Add(child);
            }
            return new PageResults(children, totalItems);
        }

        /// <summary>
        /// Called when a new file is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new file.</param>
        /// <returns>The new file.</returns>
        public async Task<IFileAsync> CreateFileAsync(string name)
        {
            await RequireHasTokenAsync();
            try
            {
                if (Path == "/")
                {
                    await CreateFileInRootAsync(name);
                }
                else
                {
                    await dataLakeDirectoryClient.CreateFileAsync(name);
                }
            }
            catch (Exception e)
            {
                throw new DavException($"Cannot create file {name}", e);
            }
            
            await context.socketService.NotifyRefreshAsync(Path);

            return (IFileAsync)await context.GetHierarchyItemAsync(Path + EncodeUtil.EncodeUrlPart(name));
        }

        /// <summary>
        /// Called when a new folder is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new folder.</param>
        public virtual async Task CreateFolderAsync(string name)
        {
            await RequireHasTokenAsync();
            // 
            if (Path == "/")
            {
                await CreateFolderInRootAsync(name);
            }
            else
            {
                await dataLakeDirectoryClient.CreateSubDirectoryAsync(name);
            }
            
            await context.socketService.NotifyRefreshAsync(Path);
        }

        /// <summary>
        /// Called when this folder is being copied.
        /// </summary>
        /// <param name="destFolder">Destination parent folder.</param>
        /// <param name="destName">New folder name.</param>
        /// <param name="deep">Whether children items shall be copied.</param>
        /// <param name="multistatus">Information about child items that failed to copy.</param>
        public override async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
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

            
            // string newDirLocalPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);
            
            // Create folder at the destination.
            try
            {
                if (await context.GetDirectoryClient(targetPath) == null)
                {
                    await targetFolder.CreateFolderAsync(destName);
                }
            }
            catch (DavException ex)
            {
                // Continue, but report error to client for the target item.
                multistatus.AddInnerException(targetPath, ex);
            }
            
            // Copy children.
            IFolderAsync createdFolder = (IFolderAsync)await context.GetHierarchyItemAsync(targetPath);
            foreach (DavHierarchyItem item in (await GetChildrenAsync(new PropertyName[0], null, null, new List<OrderProperty>())).Page)
            {
                if (!deep && item is DavFolder)
                {
                    continue;
                }
            
                try
                {
                    await item.CopyToAsync(createdFolder, item.Name, deep, multistatus);
                }
                catch (DavException ex)
                {
                    // If a child item failed to copy we continue but report error to client.
                    multistatus.AddInnerException(item.Path, ex);
                }
            }
            await context.socketService.NotifyRefreshAsync(targetFolder.Path);
        }

        /// <summary>
        /// Called when this folder is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="multistatus">Information about child items that failed to move.</param>
        public override async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            await RequireHasTokenAsync();
            if (!(destFolder is DavFolder))
            {
                throw new DavException("Target folder doesn't exist", DavStatus.CONFLICT);
            }
            
            DavFolder targetFolder = (DavFolder)destFolder;
            
            if (IsRecursive(targetFolder))
            {
                throw new DavException("Cannot move folder to its subtree.", DavStatus.FORBIDDEN);
            }
            
            // string newDirPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);
            //
            try
            {
                // Remove item with the same name at destination if it exists.
                IHierarchyItemAsync item = await context.GetHierarchyItemAsync(targetPath);
                if (item != null)
                    await item.DeleteAsync(multistatus);
            
                await targetFolder.CreateFolderAsync(destName);
            }
            catch (DavException ex)
            {
                // Continue the operation but report error with destination path to client.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }
            
            // Move child items.
            bool movedSuccessfully = true;
            IFolderAsync createdFolder = (IFolderAsync)await context.GetHierarchyItemAsync(targetPath);
            foreach (DavHierarchyItem item in (await GetChildrenAsync(new PropertyName[0], null, null, new List<OrderProperty>())).Page)
            {
                try
                {
                    await item.MoveToAsync(createdFolder, item.Name, multistatus);
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
                await DeleteAsync(multistatus);
            }
            // Refresh client UI.
            await context.socketService.NotifyDeleteAsync(Path);
            await context.socketService.NotifyRefreshAsync(GetParentPath(targetPath));
        }

        /// <summary>
        /// Called when this folder is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            /*
            if (await GetParentAsync() == null)
            {
                throw new DavException("Cannot delete root.", DavStatus.NOT_ALLOWED);
            }
            */
            await RequireHasTokenAsync();
            await dataLakeDirectoryClient.DeleteAsync();
            await context.socketService.NotifyDeleteAsync(Path);
        }

        /// <summary>
        /// Determines whether <paramref name="destFolder"/> is inside this folder.
        /// </summary>
        /// <param name="destFolder">Folder to check.</param>
        /// <returns>Returns <c>true</c> if <paramref name="destFolder"/> is inside thid folder.</returns>
        private bool IsRecursive(DavFolder destFolder)
        {
            return destFolder.Path.StartsWith(Path);
        }

        /// <summary>
        /// Sorts array of PathItem according to the specified order.
        /// </summary>
        /// <param name="fileInfos">Array of files and folders to sort.</param>
        /// <param name="orderProps">Sorting order.</param>
        /// <returns>Sorted list of files and folders.</returns>
        private IList<PathItem> SortChildren(IList<PathItem> fileInfos, IList<OrderProperty> orderProps)
        {
            if (orderProps != null && orderProps.Count() != 0)
            {
                // map DAV properties to FileSystemInfo 
                Dictionary<string, string> mappedProperties = new Dictionary<string, string>()
                { { "displayname", "Name" }, { "getlastmodified", "LastModified" }, { "getcontenttype", "Extension" },
                  { "quota-used-bytes", "ContentLength" }, { "is-directory", "IsDirectory" } };
                if (orderProps.Count != 0)
                {
                    var orderedFileInfos = fileInfos.OrderBy(p => p.Name); // init sorting by item Name
                    int index = 0;

                    foreach (OrderProperty ordProp in orderProps)
                    {
                        string propertyName = mappedProperties[ordProp.Property.Name];
                        Func<PathItem, object> sortFunc = p => p.Name; // default sorting by item Name
                        PropertyInfo propertyInfo = typeof(PathItem).GetProperties().FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.InvariantCultureIgnoreCase));

                        if (propertyInfo != null)
                        {
                            sortFunc = p => p.GetType().GetProperty(propertyInfo.Name).GetValue(p);
                        }
                        else if (propertyName == "IsDirectory")
                        {
                            sortFunc = p => p.IsDirectory;
                        }
                        else if (propertyName == "ContentLength")
                        {
                            sortFunc = p => p is PathItem ? ((PathItem)p).ContentLength : 0;
                        }
                        else if (propertyName == "Extension")
                        {
                            sortFunc = p => System.IO.Path.GetExtension(p.Name);
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

                    fileInfos = orderedFileInfos.ToList();
                }
            }

            return fileInfos;
        }

        /// <summary>
        /// Returns list of child items in current folder.
        /// </summary>
        /// <param name="client"><see cref="DataLakeDirectoryClient"/></param>
        /// <returns>Returns list of child items in current folder.</returns>
        private async Task<IList<PathItem>> GetChildrenAsync(DataLakeDirectoryClient client)
        {
            IList<PathItem> children = new List<PathItem>();
            await foreach (var pathItem in context.GetFileSystemClient().GetPathsAsync(EncodeUtil.DecodeUrlPart(client.Path)))
            {
                children.Add(pathItem);
            }
            return children;
        }

        /// <summary>
        /// Creates folder in the root folder.
        /// </summary>
        /// <param name="name">Name of new subfolder.</param>
        private async Task CreateFolderInRootAsync(string name)
        {
            await context.GetFileSystemClient().CreateDirectoryAsync(name);
        }

        /// <summary>
        /// Creates file in the root folder.
        /// </summary>
        /// <param name="name">Name of new file.</param>
        private async Task CreateFileInRootAsync(string name)
        {
            await context.GetFileSystemClient().CreateFileAsync(name);
        }

        /// <summary>
        /// Encodes parts of the path.
        /// </summary>
        /// <param name="relativePath">Relative path to encode.</param>
        private static string EncodePath(string relativePath)
        {
            string[] decodedParts = relativePath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string[] encodedParts = decodedParts.Select(EncodeUtil.EncodeUrlPart).ToArray();
            return "/" +  string.Join("/", encodedParts);
        }
    }
}
