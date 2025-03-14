
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ITHit.FileSystem.Core;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Quota;
using WebDAVServer.FileSystemSynchronization.AspNetCore.ExtendedAttributes;
using ITHit.WebDAV.Server.Search;
using ITHit.WebDAV.Server.Synchronization;
using ITHit.WebDAV.Server.ResumableUpload;
using ITHit.WebDAV.Server.Paging;

namespace WebDAVServer.FileSystemSynchronization.AspNetCore
{
    /// <summary>
    /// Folder in WebDAV repository.
    /// </summary>
    public class DavFolder : DavHierarchyItem, IFolder, IQuota, ISearch, IResumableUploadBase, ISynchronizationCollection
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
            string folderPath = context.MapPath(ref path)?.TrimEnd(System.IO.Path.DirectorySeparatorChar);
            if (string.IsNullOrEmpty(folderPath))
            {
                return null;
            }
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
            FileSystemInfo[] fileInfos = dirInfo.GetFileSystemInfos().Where(p => !p.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
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
        public async Task<IFile> CreateFileAsync(string name, Stream content, string contentType, long totalFileSize)
        {
            await RequireHasTokenAsync();
            string fileName = System.IO.Path.Combine(fileSystemInfo.FullName, name);
            // If file with same name existed here we delete it to delete all streams attached to it.
            if (File.Exists(fileName) && File.GetAttributes(fileName).HasFlag(FileAttributes.Hidden))
            {
                File.Delete(fileName);
            }
            await using (FileStream stream = new FileStream(fileName, FileMode.CreateNew))
            {
            }

            DavFile file = (DavFile)await context.GetHierarchyItemAsync(Path + EncodeUtil.EncodeUrlPart(name));
            if (content != null)
            {
                // write file content
                await file.WriteInternalAsync(content, contentType, 0, totalFileSize);
            }
            await context.socketService.NotifyCreatedAsync(System.IO.Path.Combine(Path, EncodeUtil.EncodeUrlPart(name)), GetWebSocketID());

            return file;
        }

        /// <summary>
        /// Called when a new folder is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new folder.</param>
        virtual public async Task<IFolder> CreateFolderAsync(string name)
        {
            await CreateFolderInternalAsync(name);

            DavFolder folder = (DavFolder)await context.GetHierarchyItemAsync(Path + EncodeUtil.EncodeUrlPart(name));
            await context.socketService.NotifyCreatedAsync(System.IO.Path.Combine(Path, EncodeUtil.EncodeUrlPart(name)), GetWebSocketID());

            return folder;
        }

        /// <summary>
        /// Called when a new folder is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new folder.</param>
        private async Task CreateFolderInternalAsync(string name)
        {
            await RequireHasTokenAsync();
            bool isRoot = dirInfo.Parent == null;
            DirectoryInfo di = isRoot ? new DirectoryInfo(@"\\?\" + context.RepositoryPath.TrimEnd(System.IO.Path.DirectorySeparatorChar)) : dirInfo;
            // delete hidden folder 
            string folderPath = System.IO.Path.Combine(di.FullName, name);
            if (Directory.Exists(folderPath) && new DirectoryInfo(folderPath).Attributes.HasFlag(FileAttributes.Hidden))
            {
                Directory.Delete(folderPath, true);
            }
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
            if (recursionDepth == 0)
            {
                await context.socketService.NotifyCreatedAsync(targetPath, GetWebSocketID());
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

            string newDirPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);
            try
            {
                // Remove item with the same name at destination if it exists.
                if (Directory.Exists(newDirPath))
                    Directory.Delete(newDirPath);

                dirInfo.MoveTo(newDirPath);

                // Update file system info to new.
                fileSystemInfo = new DirectoryInfo(newDirPath);
            }
            catch (DavException ex)
            {
                // Continue the operation but report error with destination path to client.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }
            await UpdateMetadateEtagAsync();
            // Refresh client UI.
            await context.socketService.NotifyMovedAsync(Path, targetPath, GetWebSocketID());
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
            await RequireHasTokenAsync();
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
                if (recursionDepth == 0)
                {
                    // hide folder, it is needed for sync-collection report.
                    dirInfo.Attributes = FileAttributes.Directory | FileAttributes.Hidden;
                    await context.socketService.NotifyDeletedAsync(Path, GetWebSocketID());
                }
                else
                {
                    dirInfo.Delete(true);
                }
                if (recursionDepth == 0)
                {
                    await context.socketService.NotifyDeletedAsync(Path, GetWebSocketID());
                }
            }
        }

        /// <summary>
        /// Returns a list of changes that correspond to a synchronization request.
        /// </summary>
        /// <param name="propNames">List of properties requested by the client.</param>
        /// <param name="syncToken">The synchronization token provided by the server and returned by the client. This method must return items that changed since this token was retuned by the server. This parameter is null or empty in case of full synchronization.</param>
        /// <param name="deep">Indicates the "scope" of the synchronization report request, false - immediate children and true - all children at any depth.</param>
        /// <param name="limit">The number of items to return. Null in case of no limit.</param>
        /// <returns>List of changes that that happened since the synchronization token provided.</returns>
        public async Task<DavChanges> GetChangesAsync(IList<PropertyName> propNames, string syncToken, bool deep, long? limit = null)
        {
            // In this sample we use item's USN as a sync token.
            // USN increases on every item update, move, creation and deletion. 

            DavChanges changes = new DavChanges();
            long syncId = string.IsNullOrEmpty(syncToken) ? 0 : long.Parse(syncToken);

            // Get all file system entries with usn.
            List<(IChangedItem HierarchyItem, long SyncId)> childrenList = new List<(IChangedItem HierarchyItem, long SyncId)>();
            foreach ((string Path, long SyncId) item in await GetSyncIdsAsync(syncId, deep))
            {
                IChangedItem child = (IChangedItem)await GetChildAsync(item.Path);

                if (child != null)
                {
                    childrenList.Add((child, item.SyncId));
                }
            }

            // If limit==0 this is a sync-token request, no need to return any changes.
            bool isSyncTokenRequest = limit.HasValue && limit.Value == 0;
            if (isSyncTokenRequest)
            {
                changes.NewSyncToken = childrenList.Max(p => p.SyncId).ToString();
                return changes;
            }

            IEnumerable<(IChangedItem HierarchyItem, long SyncId)> children = childrenList;

            // If syncId == 0 this is a full sync request.
            // We do not want to return deleted items in this case, removing them from the list.
            if (syncId == 0)
            {
                children = children.Where(item => item.HierarchyItem.ChangeType != Change.Deleted);
            }

            // Truncate results if limit is specified.
            if (limit.HasValue)
            {
                // Order children by sync ID, so we can truncate results.
                children = children.OrderBy(p => p.SyncId);

                // Truncate results.
                children = children.Take((int)limit.Value);

                // Specify if more changes can be returned.
                changes.MoreResults = limit.Value < childrenList.Count;
            }

            // Return new sync token.
            changes.NewSyncToken = children.Count() != 0 ? children.Max(p => p.SyncId).ToString() : syncToken;

            // Return changes.
            changes.AddRange(children.Select(p => p.HierarchyItem));

            return changes;
        }

        /// <summary>
        /// Creates child <see cref="IHierarchyItem"/> instance by path.
        /// </summary>
        /// <param name="childPath">Item path.</param>
        /// <returns>Instance of corresponding <see cref="IHierarchyItem"/> or null if item is not found.</returns>
        private async Task<IHierarchyItem> GetChildAsync(string childPath)
        {
            string childRelPath = childPath.Substring(dirInfo.FullName.Length).Replace(System.IO.Path.DirectorySeparatorChar.ToString(), "/").TrimStart('/');
            IEnumerable<string> encodedParts = childRelPath.Split('/').Select(EncodeUtil.EncodeUrlPart);
            string childRelUrl = Path.TrimEnd('/') + "/" + string.Join("/", encodedParts);

            DavHierarchyItem child = await DavFolder.GetFolderAsync(context, childRelUrl);
            if (child == null)
            {
                child = await DavFile.GetFileAsync(context, childRelUrl);
            }

            return child;
        }

        /// <summary>
        /// Gets all items under this folder that changed since provided sync ID.
        /// </summary>
        /// <param name="minSyncId">Synchronization token</param>
        /// <param name="deep">True if children at any depth should be returned. False - if immediate children only.</param>
        /// <returns>List of all items path under this folder and sync ID of each item.</returns>
        private async Task<IEnumerable<(string Path, long SyncId)>> GetSyncIdsAsync(long minSyncId, bool deep)
        {
            // First we must read max existing USN. However, in this sample,
            // for the sake of simplicity, we just read all changes under this folder.

            SearchOption searchOptions = deep ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

            ConcurrentBag<(string Path, long Usn)> list = new ConcurrentBag<(string Path, long Usn)>();

            string[] decendants = Directory.GetFileSystemEntries(dirInfo.FullName, "*", searchOptions);
            ParallelOptions parallelOptions = new()
            {
                MaxDegreeOfParallelism = 1000
            };
            await Parallel.ForEachAsync(decendants, parallelOptions, async (path, token) =>
            {
                long syncId = await new FileSystemItem(path).GetUsnAsync();
                if (syncId > minSyncId)
                {
                    list.Add(new(path, syncId));
                }
            });

            return list;
        }

        /// <summary>
        /// Returns free bytes available to current user.
        /// </summary>
        /// <returns>Free bytes available.</returns>
        public async Task<long> GetAvailableBytesAsync()
        {
            // Here you can return amount of bytes available for current user.
            // For the sake of simplicity we return entire available disk space.

            // Note: NTFS quotes retrieval for current user works very slowly.

            return await dirInfo.GetStorageFreeBytesAsync();
        }

        /// <summary>
        /// Returns used bytes by current user.
        /// </summary>
        /// <returns>Number of bytes used on disk.</returns>
        public async Task<long> GetUsedBytesAsync()
        {
            // Here you can return amount of bytes used by current user.
            // For the sake of simplicity we return entire used disk space.

            //Note: NTFS quotes retrieval for current user works very slowly.

            return await dirInfo.GetStorageUsedBytesAsync();
        }

        /// <summary>
        /// Searches files and folders in current folder using search phrase, offset, nResults and options.
        /// </summary>
        /// <param name="searchString">A phrase to search.</param>
        /// <param name="options">Search options.</param>
        /// <param name="propNames">
        /// List of properties to retrieve with each item returned by this method. They will be requested by the 
        /// Engine in <see cref="IHierarchyItem.GetPropertiesAsync(IList{PropertyName}, bool)"/> call.
        /// </param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <returns>List of <see cref="IHierarchyItem"/> satisfying search request.</returns>1
        /// <returns>Items satisfying search request and a total number.</returns>
        public async Task<PageResults> SearchAsync(string searchString, SearchOptions options, List<PropertyName> propNames, long? offset, long? nResults)
        {
            bool includeSnippet = propNames.Any(s => s.Name == snippetProperty);

            // search both in file name and content
            string commandText =
                @"SELECT System.ItemPathDisplay" + (includeSnippet ? " ,System.Search.AutoSummary" : string.Empty) + " FROM SystemIndex " +
                @"WHERE scope ='file:@Path' AND (System.ItemNameDisplay LIKE '@Name' OR FREETEXT('""@Content""')) " +
                @"ORDER BY System.Search.Rank DESC";

            commandText = PrepareCommand(commandText,
                "@Path", this.dirInfo.FullName,
                "@Name", searchString,
                "@Content", searchString);

            Dictionary<string, string> foundItems = new Dictionary<string, string>();
            try
            {
                // Sending SQL request to Windows Search. To get search results file system indexing must be enabled.
                // To find how to enable indexing follow this link: http://windows.microsoft.com/en-us/windows/improve-windows-searches-using-index-faq
                await using (OleDbConnection connection = new OleDbConnection(context.Config.WindowsSearchProvider))
                await using(OleDbCommand command = new OleDbCommand(commandText, connection))
                {
                    connection.Open();
                    await using(OleDbDataReader reader = command.ExecuteReader())
                    {
                        while (await reader.ReadAsync())
                        {
                            string snippet = string.Empty;
                            if (includeSnippet)
                            {
                                snippet = reader.GetValue(1) != DBNull.Value ? reader.GetString(1) : null;
                                // XML does not support control characters or permanently undefined Unicode characters. Removing them from snippet. https://www.w3.org/TR/xml/#charsets
                                if (!string.IsNullOrEmpty(snippet) && invalidXmlCharsPattern.IsMatch(snippet))
                                {
                                    snippet = invalidXmlCharsPattern.Replace(snippet, String.Empty);
                                }
                            }
                            foundItems.Add(reader.GetString(0), snippet);
                        }
                    }
                }
            }
            catch (OleDbException ex) // explaining OleDbException
            {
                context.Logger.LogError(ex.Message, ex);
                switch (ex.ErrorCode)
                {
                    case -2147217900: throw new DavException("Illegal symbols in search phrase.", DavStatus.CONFLICT);
                    default: throw new DavException("Unknown error.", DavStatus.INTERNAL_ERROR);
                }
            }

            IList<IHierarchyItem> subtreeItems = new List<IHierarchyItem>();
            foreach (string path in foundItems.Keys)
            {
                IHierarchyItem item = await context.GetHierarchyItemAsync(GetRelativePath(path)) as IHierarchyItem;
                if (item == null)
                {
                    continue;
                }

                if (includeSnippet && item is DavFile)
                    (item as DavFile).Snippet = HighlightKeywords(searchString.Trim('%'), foundItems[path]);

                subtreeItems.Add(item);
            }

            return new PageResults(offset.HasValue && nResults.HasValue ? subtreeItems.Skip((int)offset.Value).Take((int)nResults.Value) : subtreeItems, subtreeItems.Count);
            
        }
        /// <summary>
        /// Converts path on disk to encoded relative path.
        /// </summary>
        /// <param name="filePath">Path returned by Windows Search.</param>
        /// <remarks>
        /// The Search.CollatorDSO provider returns "documents" as "my documents". 
        /// There is no any real solution for this, so to build path we just replace "my documents" manually.
        /// </remarks>
        /// <returns>Returns relative encoded path for an item.</returns>
        private string GetRelativePath(string filePath)
        {
            string itemPath = filePath.ToLower().Replace("\\my documents\\", "\\documents\\");
            string repoPath = this.fileSystemInfo.FullName.ToLower().Replace("\\my documents\\", "\\documents\\");
            int relPathLength = itemPath.Substring(repoPath.Length).TrimStart('\\').Length;
            string relPath = filePath.Substring(filePath.Length - relPathLength); // to save upper symbols
            IEnumerable<string> encodedParts = relPath.Split('\\').Select(EncodeUtil.EncodeUrlPart);
            return this.Path + String.Join("/", encodedParts.ToArray());
        }

        /// <summary>
        /// Highlight the search terms in a text.
        /// </summary>
        /// <param name="keywords">Search keywords.</param>
        /// <param name="text">File content.</param>
        private static string HighlightKeywords(string searchTerms, string text)
        {
            Regex exp = new Regex(@"\b(" + string.Join("|", searchTerms.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
               // replace �\%�, �\_� and �\\� to �%�, �_� and �\�
               .Select(str => Regex.Escape(str.Replace("\\_", "_").Replace("\\%", "%").Replace("\\\\", "\\")))) + @")\b", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            return !string.IsNullOrEmpty(text) ? exp.Replace(text, "<b>$0</b>") : text;
        }

        /// <summary>
        /// Inserts parameters into the command text.
        /// </summary>
        /// <param name="commandText">Command text.</param>
        /// <param name="prms">Command parameters in pairs: name, value</param>
        /// <returns>Command text with values inserted.</returns>
        /// <remarks>
        /// The ICommandWithParameters interface is not supported by the 'Search.CollatorDSO' provider.
        /// </remarks>
        private string PrepareCommand(string commandText, params object[] prms)
        {
            if (prms.Length % 2 != 0)
                throw new ArgumentException("Incorrect number of parameters");

            for (int i = 0; i < prms.Length; i += 2)
            {
                if (!(prms[i] is string))
                    throw new ArgumentException(prms[i] + "is invalid parameter name");

                string value = (string)prms[i + 1];

                // Search.CollatorDSO provider ignores ' and " chars, but we will remove them anyway
                value = value.Replace(@"""", String.Empty);
                value = value.Replace("'", String.Empty);

                commandText = commandText.Replace((string)prms[i], value);
            }
            return commandText;
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
