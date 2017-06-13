using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Class2;
using ITHit.WebDAV.Server.MicrosoftExtensions;


namespace WebDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Base class for items like files, folders, versions etc.
    /// </summary>
    public abstract class DavHierarchyItem : IHierarchyItemAsync, ILockAsync, IMsItemAsync
    {
        /// <summary>
        /// Property name to return text anound search phrase.
        /// </summary>
        internal const string SNIPPET = "snippet";

        protected DavContext Context { get; private set; }

        public Guid ItemId { get; private set; }

        public string Name { get; private set; }

        public string Path { get; private set; }

        public DateTime Created { get; private set; }

        public DateTime Modified { get; private set; }

        protected Guid ParentId { get; private set; }
        private FileAttributes fileAttributes;

        public DavHierarchyItem(
            DavContext context,
            Guid itemId,
            Guid parentId,
            string name,
            string path,
            DateTime created,
            DateTime modified,FileAttributes fileAttributes)
        {
            this.Context = context;
            this.ItemId = itemId;
            this.ParentId = parentId;
            this.Name = name;
            this.Path = path;
            this.Created = created;
            this.Modified = modified;
            this.fileAttributes = fileAttributes;
        }

        public async Task<DavFolder> GetParentAsync()
        {
            string[] parts = Path.Trim('/').Split('/');
            string parentParentPath = "/";
            if (parts.Length >= 2)
            {
                parentParentPath = string.Join("/", parts, 0, parts.Length - 2) + "/";
                string command =
                @"SELECT 
                     ItemID
                   , ParentItemId
                   , ItemType
                   , Name
                   , Created
                   , Modified, FileAttributes                  
                  FROM Item
                  WHERE ItemId = @ItemId";

                IList<DavFolder> davFolders = await Context.ExecuteItemAsync<DavFolder>(
                    parentParentPath,
                    command,
                    "@ItemId", ParentId);
                return davFolders.FirstOrDefault();
            }
            else
            {
                return await Context.getRootFolderAsync() as DavFolder;
            }
        }

        public abstract Task CopyToAsync(
            IItemCollectionAsync destFolder,
            string destName,
            bool deep,
            MultistatusException multistatus);

        public abstract Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus);

        public abstract Task DeleteAsync(MultistatusException multistatus);

        public async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> names, bool allprop)
        {
            IList<PropertyValue> requestedPropVals = new List<PropertyValue>();
            IList<PropertyValue> propVals = await Context.ExecutePropertyValueAsync(
                    "SELECT Name, Namespace, PropVal FROM Property WHERE ItemID = @ItemID",
                    "@ItemID", ItemId);
            PropertyName snippetProperty = names.FirstOrDefault(s => s.Name == SNIPPET);
            if (allprop)
            {
                requestedPropVals= propVals;
            }
            else
            {              
                foreach (PropertyValue p in propVals)
                {
                    if (names.Contains(p.QualifiedName))
                    {
                        requestedPropVals.Add(p);
                    }
                }
            }
            if (snippetProperty.Name == SNIPPET && this is DavFile)
                 requestedPropVals.Add(new PropertyValue(snippetProperty, (this as DavFile).Snippet));
            return requestedPropVals;
        }

        public virtual async Task UpdatePropertiesAsync(
            IList<PropertyValue> setProps,
            IList<PropertyName> delProps,
            MultistatusException multistatus)
        {
            await RequireHasTokenAsync();

            foreach (PropertyValue p in setProps)
            {
                // Microsoft Mini-redirector may update file creation date, modification date and access time passing properties:
                // <Win32CreationTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:15:34 GMT</Win32CreationTime>
                // <Win32LastModifiedTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastModifiedTime>
                // <Win32LastAccessTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastAccessTime>
                // In this case update creation and modified date in your storage or do not save this properties at all, otherwise 
                // Windows Explorer will display creation and modification date from this props and it will differ from the values 
                // in the Created and Modified fields in your storage 
                if (p.QualifiedName.Namespace == "urn:schemas-microsoft-com:")
                {
                    if (p.QualifiedName.Name == "Win32CreationTime")
                    {
                        await SetDbFieldAsync("Created", DateTime.Parse(p.Value, new System.Globalization.CultureInfo("en-US")).ToUniversalTime());
                    }
                    else if (p.QualifiedName.Name == "Win32LastModifiedTime")
                    {
                        await SetDbFieldAsync("Modified", DateTime.Parse(p.Value, new System.Globalization.CultureInfo("en-US")).ToUniversalTime());
                    }
                }
                else
                {
                    await SetPropertyAsync(p); // create or update property
                }
            }

            foreach (PropertyName p in delProps)
            {
                await RemovePropertyAsync(p.Name, p.Namespace);
            }

            // You should not update modification date/time here. Mac OS X Finder expects that properties update do not change the file modification date.
            await Context.socketService.NotifyRefreshAsync(GetParentPath(Path));
        }

        public async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            IList<PropertyName> propNames = new List<PropertyName>();
            foreach (PropertyValue propName in await GetPropertiesAsync(new PropertyName[0], true))
            {
                propNames.Add(propName.QualifiedName);
            }
            return propNames;
        }
        protected async Task RequireHasTokenAsync()
        {
            if (!await ClientHasTokenAsync())
            {
                throw new LockedException();
            }
        }

        public async Task<LockResult> LockAsync(LockLevel level, bool isDeep, TimeSpan? timeout, string owner)
        {
            if (await ItemHasLockAsync(level == LockLevel.Shared))
            {
                throw new LockedException();
            }

            if (isDeep)
            {
                // check if no items are locked in this subtree
                await FindLocksDownAsync(this, level == LockLevel.Shared);
            }

            if (!timeout.HasValue || timeout == TimeSpan.MaxValue)
            {
                // If timeout is absent or infinity timeout requested,
                // grant 5 minute lock.
                timeout = TimeSpan.FromMinutes(5);
            }

            // We store expiration time in UTC. If server/database is moved 
            // to other time zone the locks expiration time is always correct.
            DateTime expires = DateTime.UtcNow + timeout.Value;

            string token = Guid.NewGuid().ToString();
            string insertLockCommand =
                @"INSERT INTO Lock (ItemID,Token,Shared,Deep,Expires,Owner)
                   VALUES(@ItemID, @Token, @Shared, @Deep, @Expires, @Owner)";

            await Context.ExecuteNonQueryAsync(
                insertLockCommand,
                "@ItemID", ItemId,
                "@Token", token,
                "@Shared", level == LockLevel.Shared,
                "@Deep", isDeep,
                "@Expires", expires,
                "@Owner", owner);
            await Context.socketService.NotifyRefreshAsync(GetParentPath(Path));

            return new LockResult(token, timeout.Value);
        }

        public async Task<RefreshLockResult> RefreshLockAsync(string token, TimeSpan? timeout)
        {
            IEnumerable<LockInfo> activeLocks = await GetActiveLocksAsync();
            LockInfo l = activeLocks.FirstOrDefault(al => al.Token == token);

            if (l == null)
            {
                throw new DavException("The lock doesn't exist", DavStatus.PRECONDITION_FAILED);
            }

            if (!timeout.HasValue || timeout == TimeSpan.MaxValue)
            {
                // If timeout is absent or infinity timeout requested,
                // grant 5 minute lock.
                l.TimeOut = TimeSpan.FromMinutes(5);
            }
            else
            {
                // Otherwise use new timeout.
                l.TimeOut = timeout.Value;
            }

            DateTime expires = DateTime.UtcNow + (TimeSpan)l.TimeOut;

            await Context.ExecuteNonQueryAsync(
                "UPDATE Lock SET Expires = @Expires WHERE Token = @Token",
                "@Expires", expires,
                "@Token", token);
            await Context.socketService.NotifyRefreshAsync(GetParentPath(Path));

            return new RefreshLockResult(l.Level, l.IsDeep, (TimeSpan)l.TimeOut, l.Owner);
        }

        public async Task UnlockAsync(string lockToken)
        {
            IEnumerable<LockInfo> activeLocks = await GetActiveLocksAsync();
            if (activeLocks.All(al => al.Token != lockToken))
            {
                throw new DavException("This lock token doesn't correspond to any lock", DavStatus.PRECONDITION_FAILED);
            }

            // remove lock from existing item
            await Context.ExecuteNonQueryAsync(
                "DELETE FROM Lock WHERE Token = @Token",
                "@Token", lockToken);
            await Context.socketService.NotifyRefreshAsync(GetParentPath(Path));
        }

        public async Task<IEnumerable<LockInfo>> GetActiveLocksAsync()
        {
            Guid entryId = ItemId;
            List<LockInfo> l = new List<LockInfo>();

            l.AddRange(GetLocks(entryId, false)); // get all locks
            while (true)
            {
                entryId = await Context.ExecuteScalarAsync<Guid>(
                    "SELECT ParentItemId FROM Item WHERE ItemId = @ItemId",
                    "@ItemId", entryId);

                if (entryId == Guid.Empty)
                {
                    break;
                }

                l.AddRange(GetLocks(entryId, true)); // get only deep locks
            }

            return l;
        }

        protected async Task SetPropertyAsync(PropertyValue prop)
        {
            string selectCommand =
                @"SELECT Count(*) FROM Property
                  WHERE ItemID = @ItemID AND Name = @Name AND Namespace = @Namespace";

            int count = await Context.ExecuteScalarAsync<int>(
                selectCommand,
                "@ItemID", ItemId,
                "@Name", prop.QualifiedName.Name,
                "@Namespace", prop.QualifiedName.Namespace);

            // insert
            if (count == 0)
            {
                string insertCommand = @"INSERT INTO Property(ItemID, Name, Namespace, PropVal)
                                          VALUES(@ItemID, @Name, @Namespace, @PropVal)";

                await Context.ExecuteNonQueryAsync(
                    insertCommand,
                    "@PropVal", prop.Value,
                    "@ItemID", ItemId,
                    "@Name", prop.QualifiedName.Name,
                    "@Namespace", prop.QualifiedName.Namespace);
            }
            else
            {
                // update
                string command = @"UPDATE Property
                      SET PropVal = @PropVal
                      WHERE ItemID = @ItemID AND Name = @Name AND Namespace = @Namespace";

                await Context.ExecuteNonQueryAsync(
                    command,
                    "@PropVal", prop.Value,
                    "@ItemID", ItemId,
                    "@Name", prop.QualifiedName.Name,
                    "@Namespace", prop.QualifiedName.Namespace);
            }
        }

        protected async Task RemovePropertyAsync(string name, string ns)
        {
            string command = @"DELETE FROM Property
                              WHERE ItemID = @ItemID
                              AND Name = @Name
                              AND Namespace = @Namespace";

            await Context.ExecuteNonQueryAsync(
                command,
                "@ItemID", ItemId,
                "@Name", name,
                "@Namespace", ns);
        }

        internal async Task<DavFolder> CopyThisItemAsync(DavFolder destFolder, DavHierarchyItem destItem, string destName)
        {
            // returns created folder, if any, otherwise null
            DavFolder createdFolder = null;

            Guid destID;
            if (destItem == null)
            {
                // copy item
                string commandText =
                    @"INSERT INTO Item(
                           ItemId
                         , Name
                         , Created
                         , Modified
                         , ParentItemId
                         , ItemType
                         , Content
                         , ContentType
                         , SerialNumber
                         , TotalContentLength
                         , LastChunkSaved
                         , FileAttributes
                         )
                      SELECT
                           @Identity
                         , @Name
                         , GETUTCDATE()
                         , GETUTCDATE()
                         , @Parent
                         , ItemType
                         , Content
                         , ContentType
                         , SerialNumber
                         , TotalContentLength
                         , LastChunkSaved
                         , FileAttributes
                      FROM Item
                      WHERE ItemId = @ItemId";

                destID = Guid.NewGuid();
                await Context.ExecuteNonQueryAsync(
                    commandText,
                    "@Name", destName,
                    "@Parent", destFolder.ItemId,
                    "@ItemId", ItemId,
                    "@Identity", destID);

                await destFolder.UpdateModifiedAsync();

                if (this is IFolderAsync)
                {
                    createdFolder = new DavFolder(
                        Context,
                        destID,
                        destFolder.ItemId,
                        destName,
                        destFolder.Path + EncodeUtil.EncodeUrlPart(destName) + "/",
                        DateTime.UtcNow,
                        DateTime.UtcNow,fileAttributes);
                }
            }
            else
            {
                // update existing destination
                destID = destItem.ItemId;

                string commandText = @"UPDATE Item SET
                                       Modified = GETUTCDATE()
                                       , ItemType = src.ItemType
                                       , ContentType = src.ContentType
                                       FROM (SELECT * FROM Item WHERE ItemId=@SrcID) src
                                       WHERE Item.ItemId=@DestID";

                await Context.ExecuteNonQueryAsync(
                    commandText,
                    "@SrcID", ItemId,
                    "@DestID", destID);

                // remove old properties from the destination
                await Context.ExecuteNonQueryAsync(
                    "DELETE FROM Property WHERE ItemID = @ItemID",
                    "@ItemID", destID);
            }

            // copy properties
            string command =
                @"INSERT INTO Property(ItemID, Name, Namespace, PropVal)
                  SELECT @DestID, Name, Namespace, PropVal
                  FROM Property
                  WHERE ItemID = @SrcID";

            await Context.ExecuteNonQueryAsync(
                command,
                "@SrcID", ItemId,
                "@DestID", destID);

            return createdFolder;
        }

        internal async Task MoveThisItemAsync(DavFolder destFolder, string destName, DavFolder parent)
        {
            string command =
                @"UPDATE Item SET
                     Name = @Name,
                     ParentItemId = @Parent
                  WHERE ItemId = @ItemID";

            await Context.ExecuteNonQueryAsync(
                command,
                "@ItemID", ItemId,
                "@Name", destName,
                "@Parent", destFolder.ItemId);

            await parent.UpdateModifiedAsync();
            await destFolder.UpdateModifiedAsync();
        }

        internal async Task DeleteThisItemAsync()
        {
            await DeleteThisItemAsync(await GetParentAsync());
        }

        internal async Task DeleteThisItemAsync(DavFolder parentFolder)
        {
            await Context.ExecuteNonQueryAsync(
                "DELETE FROM Item WHERE ItemId = @ItemID",
                "@ItemID", ItemId);

            if (parentFolder != null)
            {
                await parentFolder.UpdateModifiedAsync();
            }
        }
        private List<LockInfo> GetLocks(Guid itemId, bool onlyDeep)
        {
            if (onlyDeep)
            {
                string command =
                    @"SELECT Token, Shared, Deep, Expires, Owner
                      FROM Lock
                      WHERE ItemID = @ItemID AND Deep = @Deep AND (Expires IS NULL OR Expires > GetUtcDate())";

                return Context.ExecuteLockInfo(
                    command,
                    "@ItemID", itemId,
                    "@Deep", true);
            }

            string selectCommand =
               @"SELECT Token, Shared, Deep, Expires, Owner
                 FROM Lock
                 WHERE ItemID = @ItemID AND (Expires IS NULL OR Expires > GetUtcDate())";

            return Context.ExecuteLockInfo(
                selectCommand,
                "@ItemID", itemId);
        }

        internal async Task<bool> ClientHasTokenAsync()
        {
            IEnumerable<LockInfo> activeLocks = await GetActiveLocksAsync();
            List<LockInfo> itemLocks = activeLocks.ToList();
            if (itemLocks.Count == 0)
            {
                return true;
            }

            IList<string> clientLockTokens = Context.Request.ClientLockTokens;
            return itemLocks.Any(il => clientLockTokens.Contains(il.Token));
        }

        protected async Task<bool> ItemHasLockAsync(bool skipShared)
        {
            IEnumerable<LockInfo> activeLocks = await GetActiveLocksAsync();
            List<LockInfo> locks = activeLocks.ToList();
            if (locks.Count == 0)
            {
                return false;
            }

            return !skipShared || locks.Any(l => l.Level != LockLevel.Shared);
        }

        protected static async Task FindLocksDownAsync(IHierarchyItemAsync root, bool skipShared)
        {
            IFolderAsync folder = root as IFolderAsync;
            if (folder != null)
            {
                foreach (IHierarchyItemAsync child in await folder.GetChildrenAsync(new PropertyName[0]))
                {
                    DavHierarchyItem dbchild = child as DavHierarchyItem;
                    if (await dbchild.ItemHasLockAsync(skipShared))
                    {
                        MultistatusException mex = new MultistatusException();
                        mex.AddInnerException(dbchild.Path, new LockedException());
                        throw mex;
                    }

                    await FindLocksDownAsync(child, skipShared);
                }
            }
        }

        internal async Task UpdateModifiedAsync()
        {
            await Context.ExecuteNonQueryAsync(
                "UPDATE Item SET Modified = GETUTCDATE() WHERE ItemId = @ItemId",
                "@ItemId", ItemId);
        }

        protected string CurrentUserName
        {
            get { return Context.User != null ? Context.User.Identity.Name : string.Empty; }
        }        

        protected void SetDbField<T>(string columnName, T value)
        {
            string commandText = string.Format("UPDATE Item SET {0} = @value WHERE ItemId = @ItemId", columnName);
            Context.ExecuteNonQuery(
                commandText,
                "@value", value,
                "@ItemId", ItemId);
        }
        protected async Task SetDbFieldAsync<T>(string columnName, T value)
        {
            string commandText = string.Format("UPDATE Item SET {0} = @value WHERE ItemId = @ItemId", columnName);
            await Context.ExecuteNonQueryAsync(
                commandText,
                "@value", value,
                "@ItemId", ItemId);
        }
        public async Task<FileAttributes> GetFileAttributesAsync()
        {

            return fileAttributes;
        }

        public async Task SetFileAttributesAsync(FileAttributes value)
        {
            await SetDbFieldAsync("FileAttributes", (int)value);
        }

        /// <summary>
        /// Gets element's parent path. 
        /// </summary>
        /// <param name="path">Element's path.</param>
        /// <returns>Path to parent element.</returns>
        protected static string GetParentPath(string path)
        {
            string parentPath = $"/{path.Trim('/')}";
            int index = parentPath.LastIndexOf("/");
            parentPath = parentPath.Substring(0, index);
            return parentPath;
        }
    }
}
