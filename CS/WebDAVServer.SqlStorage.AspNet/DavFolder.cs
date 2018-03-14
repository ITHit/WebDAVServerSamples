using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Class2;
using ITHit.WebDAV.Server.Quota;
using ITHit.WebDAV.Server.Search;
using ITHit.WebDAV.Server.ResumableUpload;

namespace WebDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Represents folder in webdav repository.
    /// </summary>
    public class DavFolder : DavHierarchyItem, IFolderAsync, IQuotaAsync, ISearchAsync, IResumableUploadBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DavFolder"/> class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="itemId">Id of this folder.</param>
        /// <param name="parentId">Id of parent folder.</param>
        /// <param name="name">Name of this folder.</param>
        /// <param name="path">Encoded WebDAV path to this folder.</param>
        /// <param name="created">Date when the folder was created.</param>
        /// <param name="modified">Date when the folder was modified.</param>
        /// <param name="fileAttributes">File attributes of the folder (hidden, read-only etc.)</param>
        public DavFolder(
            DavContext context,
            Guid itemId,
            Guid parentId,
            string name,
            string path,
            DateTime created,
            DateTime modified,FileAttributes fileAttributes)
            : base(context, itemId, parentId, name, path, created, modified, fileAttributes)
        {
        }

        /// <summary>
        /// Gets child items of this folder (files or folders).
        /// </summary>
        /// <param name="props">
        /// List of properties which will be requested from children later. We don't use it here.
        /// </param>
        /// <returns>Enumerable with files and folders.</returns>
        public async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> props)
        {
            string command =
                @"SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified                    , FileAttributes                   FROM Item
                   WHERE ParentItemId = @Parent";

            return await Context.ExecuteItemAsync<IHierarchyItemAsync>(
                Path,
                command,
                "@Parent", ItemId);
        }

        /// <summary>
        /// Creates file with specified name in this folder.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <returns>Instance of <see cref="File"/> referring to newly created file.</returns>
        public async Task<IFileAsync> CreateFileAsync(string name)
        {
            if (!await ClientHasTokenAsync())
            {
                throw new LockedException();
            }
            var child = await createChildAsync(name, ItemType.File);
            await Context.socketService.NotifyRefreshAsync(Path);

            return (IFileAsync)child;
        }

        /// <summary>
        /// Creates folder with specified name in this folder.
        /// </summary>
        /// <param name="name">Name of folder to be created.</param>
        public async Task CreateFolderAsync(string name)
        {
            if (!await ClientHasTokenAsync())
            {
                throw new LockedException();
            }

            await createChildAsync(name, ItemType.Folder);
            await Context.socketService.NotifyRefreshAsync(Path);
        }

        /// <summary>
        /// Copies this folder to another folder with option to rename it.
        /// </summary>
        /// <param name="destFolder">Folder to copy this folder to.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="deep">Whether children shall be copied.</param>
        /// <param name="multistatus">Container for errors. We put here errors which occur with
        /// individual items being copied.</param>
        public override async Task CopyToAsync(
            IItemCollectionAsync destFolder,
            string destName,
            bool deep,
            MultistatusException multistatus)
        {
            DavFolder destDavFolder = destFolder as DavFolder;
            if (destFolder == null)
            {
                throw new DavException("Destination folder doesn't exist", DavStatus.CONFLICT);
            }
            if (!await destDavFolder.ClientHasTokenAsync())
            {
                throw new LockedException("Doesn't have token for destination folder.");
            }

            if (isRecursive(destDavFolder))
            {
                throw new DavException("Cannot copy folder to its subtree", DavStatus.FORBIDDEN);
            }

            IHierarchyItemAsync destItem = await destDavFolder.FindChildAsync(destName);

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

            DavFolder newDestFolder = await CopyThisItemAsync(destDavFolder, null, destName);

            // copy children
            if (deep)
            {
                foreach (IHierarchyItemAsync child in await GetChildrenAsync(new PropertyName[0]))
                {
                    var dbchild = child as DavHierarchyItem;
                    try
                    {
                        await dbchild.CopyToAsync(newDestFolder, child.Name, deep, multistatus);
                    }
                    catch (DavException ex)
                    {
                        multistatus.AddInnerException(dbchild.Path, ex);
                    }
                }
            }
            await Context.socketService.NotifyRefreshAsync(newDestFolder.Path);
        }

        /// <summary>
        /// Moves this folder to destination folder with option to rename.
        /// </summary>
        /// <param name="destFolder">Folder to copy this folder to.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="multistatus">Container for errors. We put here errors occurring while moving
        /// individual files/folders.</param>
        public override async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            DavFolder destDavFolder = destFolder as DavFolder;
            if (destFolder == null)
            {
                throw new DavException("Destination folder doesn't exist", DavStatus.CONFLICT);
            }

            if (isRecursive(destDavFolder))
            {
                throw new DavException("Cannot move folder to its subtree", DavStatus.FORBIDDEN);
            }

            DavFolder parent = await GetParentAsync();
            if (parent == null)
            {
                throw new DavException("Cannot move root", DavStatus.CONFLICT);
            }
            if (!await ClientHasTokenAsync() || !await destDavFolder.ClientHasTokenAsync() || !await parent.ClientHasTokenAsync())
            {
                throw new LockedException();
            }
     
            DavHierarchyItem destItem = await destDavFolder.FindChildAsync(destName);
            DavFolder newDestFolder;

            // copy this folder
            if (destItem != null)
            {
                if (destItem is IFileAsync)
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

                    newDestFolder = await CopyThisItemAsync(destDavFolder, null, destName);
                }
                else
                {
                    newDestFolder = destItem as DavFolder;
                    if (newDestFolder == null)
                    {
                        multistatus.AddInnerException(
                            destItem.Path,
                            new DavException("Destionation item is not folder", DavStatus.CONFLICT));
                    }
                }
            }
            else
            {
                newDestFolder = await CopyThisItemAsync(destDavFolder, null, destName);
            }

            // move children
            bool movedAllChildren = true;
            foreach (IHierarchyItemAsync child in await GetChildrenAsync(new PropertyName[0]))
            {
                DavHierarchyItem dbchild = child as DavHierarchyItem;
                try
                {
                    await dbchild.MoveToAsync(newDestFolder, child.Name, multistatus);
                }
                catch (DavException ex)
                {
                    multistatus.AddInnerException(dbchild.Path, ex);
                    movedAllChildren = false;
                }
            }

            if (movedAllChildren)
            {
                await DeleteThisItemAsync(parent);
            }
            // Refresh client UI.
            await Context.socketService.NotifyDeleteAsync(Path);
            await Context.socketService.NotifyRefreshAsync(GetParentPath(newDestFolder.Path));
        }

        /// <summary>
        /// Deletes this folder.
        /// </summary>
        /// <param name="multistatus">Container for errors.
        /// If some child file/folder fails to remove we report error in this container.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            DavFolder parent = await GetParentAsync();
            if (parent == null)
            {
                throw new DavException("Cannot delete root.", DavStatus.CONFLICT);
            }
            if (!await parent.ClientHasTokenAsync())
            {
                throw new LockedException();
            }

            if (!await ClientHasTokenAsync())
            {
                throw new LockedException();
            }

            bool deletedAllChildren = true;
            foreach (IHierarchyItemAsync child in await GetChildrenAsync(new PropertyName[0]))
            {
                DavHierarchyItem dbchild = child as DavHierarchyItem;
                try
                {
                    await dbchild.DeleteAsync(multistatus);
                }
                catch (DavException ex)
                {
                    multistatus.AddInnerException(dbchild.Path, ex);
                    deletedAllChildren = false;
                }
            }

            if (deletedAllChildren)
            {
                await DeleteThisItemAsync(parent);
                await Context.socketService.NotifyDeleteAsync(Path);
            }
        }
        /// <summary>
        /// Returns free bytes available to current user.
        /// </summary>
        /// <returns>Free bytes available.</returns>
        public async Task<long> GetAvailableBytesAsync()
        {
            //let's assume total space is 5GB.
            return (5L * 1024 * 1024 * 1024) - await GetUsedBytesAsync();
        }

        /// <summary>
        /// Returns used bytes by current user.
        /// </summary>
        /// <returns>Number of bytes consumed by files of current user.</returns>
        public async Task<long> GetUsedBytesAsync()
        {
            return await Context.ExecuteScalarAsync<long>("SELECT SUM(DATALENGTH(Content)) FROM Item");
        }
        /// <summary>
        /// Searches files and folders in current folder using search phrase and options.
        /// </summary>
        /// <param name="searchString">A phrase to search.</param>
        /// <param name="options">Search options.</param>
        /// <param name="propNames">
        /// List of properties to retrieve with each item returned by this method. They will be requested by the 
        /// Engine in <see cref="IHierarchyItemAsync.GetPropertiesAsync(IList{PropertyName}, bool)"/> call.
        /// </param>
        /// <returns>List of <see cref="IHierarchyItemAsync"/> satisfying search request.</returns>
        public async Task<IEnumerable<IHierarchyItemAsync>> SearchAsync(string searchString, SearchOptions options, List<PropertyName> propNames)
        {
            bool includeSnippet = propNames.Any(s => s.Name == SNIPPET);
            string condition = "Name LIKE @Name";

            // To enable full-text search, uncoment the code below and follow instructions 
            // in DB.sql to enable full-text indexing
            /*
            if (options.SearchContent)
            {
                condition += " OR FREETEXT(Content, '@Content')";
            }
            */

            string commandText = String.Format(
                @"SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified
                    , FileAttributes                          
                   FROM Item
                   WHERE ParentItemId = @Parent AND ({0})", condition);

            List<IHierarchyItemAsync> result = new List<IHierarchyItemAsync>();
            await GetSearchResultsAsync(result, commandText, searchString, includeSnippet);

            return result;
        }

        /// <summary>
        /// Produces recursive search in current folder.
        /// </summary>
        /// <param name="result">A list to add search results to.</param>
        /// <param name="commandText">SQL command text for search in a folder.</param>
        /// <param name="searchString">A phrase to search.</param>
        private async Task GetSearchResultsAsync(List<IHierarchyItemAsync> result, string commandText, string searchString, bool includeSnippet)
        {
            // search this folder
            IEnumerable<IHierarchyItemAsync> folderSearchResults = await Context.ExecuteItemAsync<IHierarchyItemAsync>(
                Path,
                commandText,
                "@Parent", ItemId,
                "@Name", searchString,
                "@Content", searchString);

            foreach (IHierarchyItemAsync item in folderSearchResults)
            {
                if (includeSnippet && item is DavFile)
                    (item as DavFile).Snippet = "Not Implemented";
            }
            result.AddRange(folderSearchResults);

            // search children
            foreach (IHierarchyItemAsync item in await GetChildrenFoldersAsync())
            {
                DavFolder folder = item as DavFolder;
                if (folder != null)
                    await folder.GetSearchResultsAsync(result, commandText, searchString, includeSnippet);
            }            
        }

        /// <summary>
        /// Gets the children of current folder (non-recursive).
        /// </summary>
        /// <returns>The children folders of current folder.</returns>
        public async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenFoldersAsync()
        {
            string command =
                @"SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified
                    , FileAttributes
                   FROM Item
                   WHERE ParentItemId = @Parent AND ItemType = 3"; // folders only

            return await Context.ExecuteItemAsync<IHierarchyItemAsync>(
                Path,
                command,
                "@Parent", ItemId);
        }

        /// <summary>
        /// Finds file or folder with specified name inside this folder.
        /// </summary>
        /// <param name="childName">Name of child to find.</param>
        /// <returns>Instance of <see cref="DavHierarchyItem"/> or <c>null</c>.</returns>
        internal async Task<DavHierarchyItem> FindChildAsync(string childName)
        {
            string commandText =
                @"SELECT
                     ItemId
                   , ParentItemId
                   , ItemType
                   , Name
                   , Created
                   , Modified 
                   , FileAttributes  
                  FROM Item
                  WHERE ParentItemId = @Parent
                  AND Name = @Name";
            IList<DavHierarchyItem> davHierarchyItems = await Context.ExecuteItemAsync<DavHierarchyItem>(
                Path,
                commandText,
                "@Parent", ItemId,
                "@Name", childName);
            return davHierarchyItems.FirstOrDefault();
        }
        /// <summary>
        /// Determines whether the client has submitted lock tokens for all locked files in the subtree.
        /// </summary>
        /// <returns>Returns <c>true</c> if lock tockens for all locked files in the subtree are submitted.</returns>
        internal async Task<bool> ClientHasTokenForTreeAsync()
        {
            if (!await ClientHasTokenAsync())
            {
                return false;
            }

            foreach (IHierarchyItemAsync child in await GetChildrenAsync(new PropertyName[0]))
            {
                DavFolder childFolder = child as DavFolder;
                if (childFolder != null)
                {
                    if (!await childFolder.ClientHasTokenForTreeAsync())
                    {
                        return false;
                    }
                }
                else
                {
                    DavHierarchyItem childItem = child as DavHierarchyItem;
                    if (!await childItem.ClientHasTokenAsync())
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Determines whether <paramref name="destFolder"/> is inside this folder.
        /// </summary>
        /// <param name="destFolder">Folder to test.</param>
        /// <returns>Returns <c>true</c>if <paramref name="destFolder"/> is inside this folder.</returns>
        private bool isRecursive(DavFolder destFolder)
        {
            return destFolder.Path.StartsWith(Path);
        }

        /// <summary>
        /// Creates file or folder with specified name inside this folder.
        /// </summary>
        /// <param name="name">File/folder name.</param>
        /// <param name="itemType">Type of item: file or folder.</param>
        /// <returns>Newly created item.</returns>
        private async Task<DavHierarchyItem> createChildAsync(string name, ItemType itemType)
        {
            Guid newID = Guid.NewGuid();
            string commandText =
                @"INSERT INTO Item(
                      ItemId
                    , Name
                    , Created
                    , Modified
                    , ParentItemId
                    , ItemType
                    , TotalContentLength
                    , FileAttributes
                    )
                VALUES(
                     @Identity
                   , @Name
                   , GETUTCDATE()
                   , GETUTCDATE()
                   , @Parent
                   , @ItemType
                   , 0
                   , @FileAttributes
                   )";

            await Context.ExecuteNonQueryAsync(
                commandText,
                "@Name", name,
                "@Parent", ItemId,
                "@ItemType", itemType,
                "@FileAttributes", (itemType == ItemType.Folder ? (int)FileAttributes.Directory : (int)FileAttributes.Normal),
                "@Identity", newID);

            //UpdateModified(); do not update time for folder as transaction will block concurrent files upload
            switch (itemType)
            {
                case ItemType.File:
                    return new DavFile(
                        Context,
                        newID,
                        ItemId,
                        name,
                        Path + EncodeUtil.EncodeUrlPart(name),
                        DateTime.UtcNow,
                        DateTime.UtcNow,FileAttributes.Normal);
                case ItemType.Folder:
                    // do not need to return created folder
                    return null;
                default:
                    return null;
            }
        }
    }
}
