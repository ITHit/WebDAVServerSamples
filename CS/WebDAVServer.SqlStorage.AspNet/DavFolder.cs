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
using ITHit.WebDAV.Server.Paging;

namespace WebDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Represents folder in webdav repository.
    /// </summary>
    public class DavFolder : DavHierarchyItem, IFolder, IQuota, ISearch, IResumableUploadBase
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
        /// Called when children of this folder with paging information are being listed.
        /// </summary>
        /// <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Items requested by the client and a total number of items in this folder.</returns>
        public virtual async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            IList<IHierarchyItem> children = null;

            if (orderProps != null && orderProps.Count() != 0 && nResults.HasValue && offset.HasValue)
            {
                // map DAV properties to db table 
                Dictionary<string, string> mappedProperties = new Dictionary<string, string>()
                { { "displayname", "Name" }, { "getlastmodified", "Modified" }, { "getcontenttype", "(case when Name like '%.%' then reverse(left(reverse(Name), charindex('.', reverse(Name)) - 1)) else '' end)" },
                  { "quota-used-bytes", "(DATALENGTH(Content))" }, { "is-directory", "IIF(ItemType = 3, 1, 0)" } };
                List<string> orderByProperies = new List<string>();

                foreach (OrderProperty ordProp in orderProps)
                {
                    orderByProperies.Add(string.Format("{0} {1}", mappedProperties[ordProp.Property.Name], ordProp.Ascending ? "ASC" : "DESC"));
                }

                string command =
                    String.Format(@"SELECT * FROM (SELECT 
                    ROW_NUMBER() OVER (ORDER BY {0}) AS RowNum
                    ,ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified                    , FileAttributes                   FROM Item
                   WHERE ParentItemId = @Parent) AS PageResults WHERE RowNum >= @StartRow
                   AND RowNum <= @EndRow
                   ORDER BY RowNum", string.Join(",", orderByProperies));

               children = await Context.ExecuteItemAsync<IHierarchyItem>(
                    Path,
                    command,
                    "@Parent", ItemId,
                    "@StartRow", offset + 1,
                    "@EndRow", offset + nResults);
            }
            else
            {
                string command =
                  @"SELECT 
                          ItemId
                        , ParentItemId
                        , ItemType
                        , Name
                        , Created
                        , Modified                        , FileAttributes                       FROM Item
                       WHERE ParentItemId = @Parent";

                children = await Context.ExecuteItemAsync<IHierarchyItem>(
                    Path,
                    command,
                    "@Parent", ItemId);
            }

            return new PageResults(children, await Context.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Item WHERE ParentItemId = @Parent", "@Parent", ItemId));
        }

        /// <summary>
        /// Creates file with specified name in this folder.
        /// </summary>
        /// <param name="name">File name.</param>
        /// <param name="content">Stream to read the content of the file from.</param>
        /// <param name="contentType">Indicates the media type of the file.</param>
        /// <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        /// <returns>Instance of <see cref="File"/> referring to newly created file.</returns>
        public async Task<IFile> CreateFileAsync(string name, Stream content, string contentType, long totalFileSize)
        {
            if (!await ClientHasTokenAsync())
            {
                throw new LockedException();
            }
            DavFile file = (DavFile)await createChildAsync(name, ItemType.File);
            // write file content
            await file.WriteInternalAsync(content, contentType, 0, totalFileSize);
            await Context.socketService.NotifyCreatedAsync(System.IO.Path.Combine(Path, EncodeUtil.EncodeUrlPart(name)), GetWebSocketID());

            return (IFile)file;
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
            await Context.socketService.NotifyCreatedAsync(System.IO.Path.Combine(Path, EncodeUtil.EncodeUrlPart(name)), GetWebSocketID());
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
            IItemCollection destFolder,
            string destName,
            bool deep,
            MultistatusException multistatus)
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

            DavHierarchyItem destItem = await destDavFolder.FindChildAsync(destName);

            if (destItem != null)
            {
                try
                {
                    await destItem.DeleteInternalAsync(multistatus, recursionDepth + 1);
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
                foreach (DavHierarchyItem child in (await GetChildrenAsync(new PropertyName[0], null, null, null)).Page)
                {
                    try
                    {
                        await child.CopyToInternalAsync(newDestFolder, child.Name, deep, multistatus, recursionDepth + 1);
                    }
                    catch (DavException ex)
                    {
                        multistatus.AddInnerException(child.Path, ex);
                    }
                }
            }
            if (recursionDepth == 0)
            {
                await Context.socketService.NotifyCreatedAsync(newDestFolder.Path, GetWebSocketID());
            }
        }

        /// <summary>
        /// Moves this folder to destination folder with option to rename.
        /// </summary>
        /// <param name="destFolder">Folder to copy this folder to.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="multistatus">Container for errors. We put here errors occurring while moving
        /// individual files/folders.</param>
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
                if (destItem is IFile)
                {
                    try
                    {
                        await destItem.DeleteInternalAsync(multistatus, recursionDepth + 1);
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
            foreach (DavHierarchyItem child in (await GetChildrenAsync(new PropertyName[0], null, null, null)).Page)
            {
                try
                {
                    await child.MoveToInternalAsync(newDestFolder, child.Name, multistatus, recursionDepth + 1);
                }
                catch (DavException ex)
                {
                    multistatus.AddInnerException(child.Path, ex);
                    movedAllChildren = false;
                }
            }

            if (movedAllChildren)
            {
                await DeleteThisItemAsync(parent);
            }
            if (recursionDepth == 0)
            {
                // Refresh client UI.
                await Context.socketService.NotifyMovedAsync(Path, newDestFolder.Path, GetWebSocketID());
            }
        }

        /// <summary>
        /// Deletes this folder.
        /// </summary>
        /// <param name="multistatus">Container for errors.
        /// If some child file/folder fails to remove we report error in this container.</param>
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
            foreach (DavHierarchyItem child in (await GetChildrenAsync(new PropertyName[0], null, null, null)).Page)
            {
                try
                {
                    await child.DeleteInternalAsync(multistatus, recursionDepth + 1);
                }
                catch (DavException ex)
                {
                    multistatus.AddInnerException(child.Path, ex);
                    deletedAllChildren = false;
                }
            }

            if (deletedAllChildren)
            {
                await DeleteThisItemAsync(parent);
                if (recursionDepth == 0)
                {
                    await Context.socketService.NotifyDeletedAsync(Path, GetWebSocketID());
                }
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
            bool includeSnippet = propNames.Any(s => s.Name == SNIPPET);

            string commandText = @"
                ;WITH Hierarchy
                AS (
                SELECT 
                      ItemId
                    , ParentItemId
                    , ItemType
                    , Name
                    , Created
                    , Modified
                    , FileAttributes
                    , RelativePath = Cast(Name as nvarchar)
                FROM Item
                Where ParentItemId = @Parent
                UNION ALL
                SELECT 
                     Child.ItemId
                    , Child.ParentItemId
                    , Child.ItemType
                    , Child.Name
                    , Child.Created
                    , Child.Modified
                    , Child.FileAttributes
                    , RelativePath = Cast(Concat(RelativePath, '/',  Child.Name) as nvarchar)
                FROM Item Child
                Join Hierarchy Parent ON Child.ParentItemId = Parent.ItemId
                )";

            // To disable full-text search, uncomment the code below and comment next code or follow instructions 
            // in DB.sql to enable full-text indexing
            //commandText += @"
            //    SELECT
            //          *
            //        , TotalRowsCount = COUNT(*) OVER()
            //    FROM Hierarchy
            //    Where Name Like @SearchString  
            //    ORDER BY Name Asc";

            // To disable full-text search, comment the code below or follow instructions 
            // in DB.sql to enable full-text indexing
            commandText += @"
                SELECT
                      *
                    , RANK
                    , TotalRowsCount = COUNT(*) OVER()
                FROM Hierarchy AS ItemTable   
                Left JOIN  
                FREETEXTTABLE(Item, Content, @SearchString) AS RankTable  
                ON ItemTable.ItemId = RankTable.[KEY]
                Where RANK Is Not null OR Name Like @SearchString  
                ORDER BY  -RANK, Name Asc";

            try
            {
                return await GetSearchResultsAsync(commandText, searchString, includeSnippet, offset, nResults);
            }
            catch (System.Data.SqlClient.SqlException e)
            {
                if(e.Message.Contains(("FREETEXT")))
                {
                    throw new DavException("Full text search is disabled. To enable full text search refer to the instructions in SQL configuration file.", e);
                }

                throw;
            }
        }

        /// <summary>
        /// Produces recursive search in current folder.
        /// </summary>
        /// <param name="commandText">SQL command text for search in a folder.</param>
        /// <param name="searchString">A phrase to search.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        private async Task<PageResults> GetSearchResultsAsync(string commandText, string searchString, bool includeSnippet, long? offset, long? nResults)
        {
            PageResults folderSearchResults = await Context.ExecuteItemPagedHierarchyAsync(
                Path,
                commandText,
                offset,
                nResults,
                "@Parent", ItemId,
                "@SearchString", searchString);

            foreach (IHierarchyItem item in folderSearchResults.Page)
            {
                if (includeSnippet && item is DavFile)
                    (item as DavFile).Snippet = "Not Implemented";
            }

            return folderSearchResults;
        }

        /// <summary>
        /// Gets the children of current folder (non-recursive).
        /// </summary>
        /// <returns>The children folders of current folder.</returns>
        public async Task<IEnumerable<IHierarchyItem>> GetChildrenFoldersAsync()
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

            return await Context.ExecuteItemAsync<IHierarchyItem>(
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

            foreach (IHierarchyItem child in (await GetChildrenAsync(new PropertyName[0], null, null, null)).Page)
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
