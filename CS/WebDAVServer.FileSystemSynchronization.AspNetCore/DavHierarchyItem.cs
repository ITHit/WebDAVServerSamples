using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal;
using ITHit.WebDAV.Server.Extensibility;
using ITHit.WebDAV.Server.Class2;
using ITHit.WebDAV.Server.MicrosoftExtensions;
using ITHit.WebDAV.Server.Synchronization;
using WebDAVServer.FileSystemSynchronization.AspNetCore.ExtendedAttributes;

namespace WebDAVServer.FileSystemSynchronization.AspNetCore
{
    /// <summary>
    /// Base class for WebDAV items (folders, files, etc).
    /// </summary>
    public abstract class DavHierarchyItem : IHierarchyItem, ILock, IMsItem, IBind, IChangedItem
    {
        /// <summary>
        /// Property name to return text anound search phrase.
        /// </summary>
        internal const string snippetProperty = "snippet";
        /// <summary>
        /// Property name of metadata Etag.
        /// </summary>
        internal const string metadataEtagProperty = "metadata-Etag";

        /// <summary>
        /// Name of properties attribute.
        /// </summary>
        internal const string propertiesAttributeName = "Properties";

        /// <summary>
        /// Name of locks attribute.
        /// </summary>
        internal const string locksAttributeName = "Locks";

        /// <summary>
        /// Gets name of the item.
        /// </summary>
        public string Name { get { return fileSystemInfo.Name; } }

        /// <summary>
        /// Gets date when the item was created in UTC.
        /// </summary>
        public DateTime Created { get { return fileSystemInfo.CreationTimeUtc; } }

        /// <summary>
        /// Gets date when the item was last modified in UTC.
        /// </summary>
        public DateTime Modified { get { return fileSystemInfo.LastWriteTimeUtc; } }

        /// <summary>
        /// Gets path of the item where each part between slashes is encoded.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets full path for this file/folder in the file system.
        /// </summary>
        public string FullPath { get { return fileSystemInfo.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar); } }
        public Change ChangeType
        {
            get => fileSystemInfo.Attributes.HasFlag(FileAttributes.Hidden) &&
                (this is DavFolder || (this as DavFile).ContentLength == 0) ? Change.Deleted : Change.Changed;
        }

        /// <summary>
        /// Unique identifier of the resource.
        /// </summary>
        public string Id { get => $"{context.Request.UrlPrefix}/ID/{fileSystemInfo.GetId()}"; }

        /// <summary>
        /// Unique identifier of the resource parent. 
        /// </summary>
        public string ParentId { get => $"{context.Request.UrlPrefix}/ID/{Directory.GetParent(fileSystemInfo.FullName).GetId()}"; }

        /// <summary>
        /// Corresponding file or folder in the file system.
        /// </summary>
        internal FileSystemInfo fileSystemInfo;

        /// <summary>
        /// WebDAV Context.
        /// </summary>
        protected DavContext context;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="fileSystemInfo">Corresponding file or folder in the file system.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        protected DavHierarchyItem(FileSystemInfo fileSystemInfo, DavContext context, string path)
        {
            this.fileSystemInfo = fileSystemInfo;
            this.context = context;
            this.Path = path;
        }

        /// <summary>
        /// Creates a copy of this item with a new name in the destination folder.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">Name of the destination item.</param>
        /// <param name="deep">Indicates whether to copy entire subtree.</param>
        /// <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using 
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        public abstract Task CopyToAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus);

        /// <summary>
        /// Creates a copy of this item with a new name in the destination folder.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">Name of the destination item.</param>
        /// <param name="deep">Indicates whether to copy entire subtree.</param>
        /// <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using 
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public abstract Task CopyToInternalAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus, int recursionDepth);

        /// <summary>
        /// Moves this item to the destination folder under a new name.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">Name of the destination item.</param>
        /// <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using 
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        public abstract Task MoveToAsync(IItemCollection destFolder, string destName, MultistatusException multistatus);

        /// <summary>
        /// Moves this item to the destination folder under a new name.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">Name of the destination item.</param>
        /// <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using 
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public abstract Task MoveToInternalAsync(IItemCollection destFolder, string destName, MultistatusException multistatus, int recursionDepth);

        /// <summary>
        /// Deletes this item.
        /// </summary>
        /// <param name="multistatus">If some items fail to delete but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        public abstract Task DeleteAsync(MultistatusException multistatus);

        /// <summary>
        /// Deletes this item.
        /// </summary>
        /// <param name="multistatus">If some items fail to delete but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        /// <param name="recursionDepth">Recursion depth.</param>
        public abstract Task DeleteInternalAsync(MultistatusException multistatus, int recursionDepth);

        /// <summary>
        /// Retrieves user defined property values.
        /// </summary>
        /// <param name="names">Names of dead properties which values to retrieve.</param>
        /// <param name="allprop">Whether all properties shall be retrieved.</param>
        /// <returns>Property values.</returns>
        public async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> props, bool allprop)
        {
            List<PropertyValue> propertyValues = await GetPropertyValuesAsync();

            PropertyName snippet = props.FirstOrDefault(s => s.Name == snippetProperty);
            if (snippet.Name == snippetProperty && this is DavFile)
            {
                propertyValues.Add(new PropertyValue(snippet, ((DavFile)this).Snippet));
            }
            PropertyName metadataEtag = props.FirstOrDefault(s => s.Name == metadataEtagProperty);
            if (metadataEtag.Name == metadataEtagProperty)
            {
                propertyValues.Add(new PropertyValue(metadataEtag,
                    ((await fileSystemInfo.GetExtendedAttributeAsync<int?>("MetadateSerialNumber")) ?? 0).ToString()));
            }
            if (!allprop)
            {
                propertyValues = propertyValues.Where(p => props.Contains(p.QualifiedName)).ToList();
            }

            return propertyValues;
        }

        /// <summary>
        /// Retrieves names of all user defined properties.
        /// </summary>
        /// <returns>Property names.</returns>
        public async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            IList<PropertyValue> propertyValues = await GetPropertyValuesAsync();
            return propertyValues.Select(p => p.QualifiedName);
        }

        /// <summary>
        /// Retrieves list of user defined propeties for this item.
        /// </summary>
        /// <returns>List of user defined properties.</returns>
        private async Task<List<PropertyValue>> GetPropertyValuesAsync()
        {
            List<PropertyValue> properties = new List<PropertyValue>();
            if (await fileSystemInfo.HasExtendedAttributeAsync(propertiesAttributeName))
            {
                properties = await fileSystemInfo.GetExtendedAttributeAsync<List<PropertyValue>>(propertiesAttributeName);
            }

            return properties;
        }

        /// <summary>
        /// Saves property values to extended attribute.
        /// </summary>
        /// <param name="setProps">Properties to be set.</param>
        /// <param name="delProps">Properties to be deleted.</param>
        /// <param name="multistatus">Information about properties that failed to create, update or delate.</param>
        public async Task UpdatePropertiesAsync(IList<PropertyValue> setProps, IList<PropertyName> delProps, MultistatusException multistatus)
        {
            await RequireHasTokenAsync();
            List<PropertyValue> propertyValues = await GetPropertyValuesAsync();
            foreach (PropertyValue propToSet in setProps)
            {
                // Microsoft Mini-redirector may update file creation date, modification date and access time passing properties:
                // <Win32CreationTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:15:34 GMT</Win32CreationTime>
                // <Win32LastModifiedTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastModifiedTime>
                // <Win32LastAccessTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastAccessTime>
                // In this case update creation and modified date in your storage or do not save this properties at all, otherwise 
                // Windows Explorer will display creation and modification date from this props and it will differ from the values 
                // in the Created and Modified fields in your storage 
                if (propToSet.QualifiedName.Namespace == "urn:schemas-microsoft-com:")
                {
                    switch (propToSet.QualifiedName.Name)
                    {
                        case "Win32CreationTime":
                            fileSystemInfo.CreationTimeUtc = DateTime.Parse(propToSet.Value,
                                new System.Globalization.CultureInfo("en-US")).ToUniversalTime();
                            break;
                        case "Win32LastModifiedTime":
                            fileSystemInfo.LastWriteTimeUtc = DateTime.Parse(propToSet.Value,
                                new System.Globalization.CultureInfo("en-US")).ToUniversalTime();
                            break;
                        default:
                            context.Logger.LogDebug(string.Format("Unspecified case: DavHierarchyItem.UpdateProperties {0} from {1} namesapce",
                                propToSet.QualifiedName.Name, propToSet.QualifiedName.Namespace));
                            break;
                    }
                }
                else
                {
                    PropertyValue existingProp = propertyValues.FirstOrDefault(p => p.QualifiedName == propToSet.QualifiedName);

                    if (existingProp != null)
                    {
                        existingProp.Value = propToSet.Value;
                    }
                    else
                    {
                        propertyValues.Add(propToSet);
                    }
                }
            }

            propertyValues.RemoveAll(prop => delProps.Contains(prop.QualifiedName));

            await fileSystemInfo.SetExtendedAttributeAsync(propertiesAttributeName, propertyValues);
            await UpdateMetadateEtagAsync();
            await context.socketService.NotifyUpdatedAsync(Path, GetWebSocketID());
        }

        /// <summary>
        /// Returns Windows file attributes (readonly, hidden etc.) for this file/folder.
        /// </summary>
        /// <returns>Windows file attributes.</returns>
        public async Task<FileAttributes> GetFileAttributesAsync()
        {
            if (Name.StartsWith("."))
            {
                return fileSystemInfo.Attributes | FileAttributes.Hidden;
            }
            return fileSystemInfo.Attributes;
        }

        /// <summary>
        /// Sets Windows file attributes (readonly, hidden etc.) on this item.
        /// </summary>
        /// <param name="value">File attributes.</param>
        public async Task SetFileAttributesAsync(FileAttributes value)
        {
            File.SetAttributes(fileSystemInfo.FullName, value);
        }

        /// <summary>
        /// Retrieves non expired locks for this item.
        /// </summary>
        /// <returns>Enumerable with information about locks.</returns>
        public async Task<IEnumerable<LockInfo>> GetActiveLocksAsync()
        {
            List<DateLockInfo> locks = await GetLocksAsync();
            if (locks == null)
            {
                return new List<LockInfo>();
            }

            IEnumerable<LockInfo> lockInfoList = locks.Select(l => new LockInfo
            {
                IsDeep = l.IsDeep,
                Level = l.Level,
                Owner = l.ClientOwner,
                LockRoot = l.LockRoot,
                TimeOut = l.Expiration == DateTime.MaxValue ?
                            TimeSpan.MaxValue :
                            l.Expiration - DateTime.UtcNow,
                Token = l.LockToken
            }).ToList();

            return lockInfoList;
        }

        /// <summary>
        /// Locks this item.
        /// </summary>
        /// <param name="level">Whether lock is share or exclusive.</param>
        /// <param name="isDeep">Whether lock is deep.</param>
        /// <param name="requestedTimeOut">Lock timeout which was requested by client.
        /// Server may ignore this parameter and set any timeout.</param>
        /// <param name="owner">Owner of the lock as specified by client.</param> 
        /// <returns>
        /// Instance of <see cref="LockResult"/> with information about the lock.
        /// </returns>
        public async Task<LockResult> LockAsync(LockLevel level, bool isDeep, TimeSpan? requestedTimeOut, string owner)
        {
            await RequireUnlockedAsync(level == LockLevel.Shared);
            string token = Guid.NewGuid().ToString();

            // If timeout is absent or infinit timeout requested,
            // grant 5 minute lock.
            TimeSpan timeOut = TimeSpan.FromMinutes(5);

            if (requestedTimeOut.HasValue && requestedTimeOut < TimeSpan.MaxValue)
            {
                timeOut = requestedTimeOut.Value;
            }

            DateLockInfo lockInfo = new DateLockInfo
            {
                Expiration = DateTime.UtcNow + timeOut,
                IsDeep = false,
                Level = level,
                LockRoot = Path,
                LockToken = token,
                ClientOwner = owner,
                TimeOut = timeOut
            };

            await SaveLockAsync(lockInfo);
            await context.socketService.NotifyLockedAsync(Path, GetWebSocketID());

            return new LockResult(lockInfo.LockToken, lockInfo.TimeOut);
        }

        /// <summary>
        /// Updates lock timeout information on this item.
        /// </summary>
        /// <param name="token">Lock token.</param>
        /// <param name="requestedTimeOut">Lock timeout which was requested by client.
        /// Server may ignore this parameter and set any timeout.</param>
        /// <returns>
        /// Instance of <see cref="LockResult"/> with information about the lock.
        /// </returns>
        public async Task<RefreshLockResult> RefreshLockAsync(string token, TimeSpan? requestedTimeOut)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new DavException("Lock can not be found.", DavStatus.BAD_REQUEST);
            }

            List<DateLockInfo> locks = await GetLocksAsync(getAllWithExpired: true);
            DateLockInfo lockInfo = locks.SingleOrDefault(x => x.LockToken == token);

            if (lockInfo == null || lockInfo.Expiration <= DateTime.UtcNow)
            {
                throw new DavException("Lock can not be found.", DavStatus.CONFLICT);
            }
            else
            {
                lockInfo.TimeOut = TimeSpan.FromMinutes(5);

                if (requestedTimeOut.HasValue && requestedTimeOut < TimeSpan.MaxValue)
                {
                    lockInfo.TimeOut = requestedTimeOut.Value;
                }


                lockInfo.Expiration = DateTime.UtcNow + lockInfo.TimeOut;

                await SaveLockAsync(lockInfo);
            }
            await context.socketService.NotifyLockedAsync(Path, GetWebSocketID());

            return new RefreshLockResult(lockInfo.Level, lockInfo.IsDeep, lockInfo.TimeOut, lockInfo.ClientOwner);
        }

        /// <summary>
        /// Removes lock with the specified token from this item.
        /// </summary>
        /// <param name="lockToken">Lock with this token should be removed from the item.</param>
        public async Task UnlockAsync(string lockToken)
        {
            if (string.IsNullOrEmpty(lockToken))
            {
                throw new DavException("Lock can not be found.", DavStatus.BAD_REQUEST);
            }

            List<DateLockInfo> locks = await GetLocksAsync(getAllWithExpired: true);
            DateLockInfo lockInfo = locks.SingleOrDefault(x => x.LockToken == lockToken);

            await RemoveExpiredLocksAsync(lockToken);

            if (lockInfo == null || lockInfo.Expiration <= DateTime.UtcNow)
            {
                throw new DavException("The lock could not be found.", DavStatus.CONFLICT);
            }
            await context.socketService.NotifyUnLockedAsync(Path, GetWebSocketID());
        }

        /// <summary>
        /// Check that if the item is locked then client has submitted correct lock token.
        /// </summary>
        public async Task RequireHasTokenAsync(bool skipShared = false)
        {
            List<DateLockInfo> locks = await GetLocksAsync();
            if (locks != null && locks.Any())
            {
                IList<string> clientLockTokens = context.Request.GetClientLockTokens();
                if (locks.All(l => !clientLockTokens.Contains(l.LockToken)))
                {
                    throw new LockedException();
                }
            }
        }

        /// <summary>
        /// Ensure that there are no active locks on the item.
        /// </summary>
        /// <param name="skipShared">Whether shared locks shall be checked.</param>
        public async Task RequireUnlockedAsync(bool skipShared)
        {
            List<DateLockInfo> locks = await GetLocksAsync();

            if (locks != null && locks.Any())
            {
                if ((skipShared && locks.Any(l => l.Level == LockLevel.Exclusive))
                    || (!skipShared && locks.Any()))
                {
                    throw new LockedException();
                }
            }
        }

        /// <summary>
        /// Retrieves non-expired locks acquired on this item.
        /// </summary>
        /// <param name="getAllWithExpired">Indicate needed return expired locks.</param>
        /// <returns>List of locks with their expiration dates.</returns>
        private async Task<List<DateLockInfo>> GetLocksAsync(bool getAllWithExpired = false)
        {

            List<DateLockInfo> locks = new List<DateLockInfo>();

            if (await fileSystemInfo.HasExtendedAttributeAsync(locksAttributeName))
            {
                locks = await fileSystemInfo.GetExtendedAttributeAsync<List<DateLockInfo>>(locksAttributeName);

                if (locks != null)
                {
                    locks.ForEach(l => l.LockRoot = Path);
                }
            }

            if (getAllWithExpired)
            {
                return locks;
            }
            else
            {
                return locks.Where(x => x.Expiration > DateTime.UtcNow).ToList();
            }
        }

        /// <summary>
        /// Saves lock acquired on this file/folder.
        /// </summary>
        private async Task SaveLockAsync(DateLockInfo lockInfo)
        {
            List<DateLockInfo> locks = await GetLocksAsync(getAllWithExpired: true);

            //remove all expired locks
            //await RemoveExpiretLocksAsync();
            //you can call this method but it will be second file operation
            locks.RemoveAll(x => x.Expiration <= DateTime.UtcNow);

            if (locks.Any(x => x.LockToken == lockInfo.LockToken))
            {
                //update value
                DateLockInfo existingLock = locks.Single(x => x.LockToken == lockInfo.LockToken);
                existingLock.TimeOut = lockInfo.TimeOut;

                existingLock.Level = lockInfo.Level;
                existingLock.IsDeep = lockInfo.IsDeep;
                existingLock.LockRoot = lockInfo.LockRoot;
                existingLock.Expiration = lockInfo.Expiration;
                existingLock.ClientOwner = lockInfo.ClientOwner;
            }
            else
            {
                //add new item
                locks.Add(lockInfo);
            }

            await fileSystemInfo.SetExtendedAttributeAsync(locksAttributeName, locks);
            await UpdateMetadateEtagAsync();
        }

        private async Task RemoveExpiredLocksAsync(string unlockedToken)
        {
            List<DateLockInfo> locks = await GetLocksAsync(getAllWithExpired: true);

            //remove expired and current lock
            locks.RemoveAll(x => x.Expiration <= DateTime.UtcNow);

            //remove from token
            if (!string.IsNullOrEmpty(unlockedToken))
            {
                locks.RemoveAll(x => x.LockToken == unlockedToken);
            }

            await fileSystemInfo.SetExtendedAttributeAsync(locksAttributeName, locks);
            await UpdateMetadateEtagAsync();
        }

        /// <summary>
        /// Gets element's parent path. 
        /// </summary>
        /// <param name="path">Element's path.</param>
        /// <returns>Path to parent element.</returns>
        protected static string GetParentPath(string path)
        {
            string parentPath = string.Format("/{0}", path.Trim('/'));
            int index = parentPath.LastIndexOf("/");
            parentPath = parentPath.Substring(0, index);
            return parentPath;
        }

        /// <summary>
        /// Returns WebSocket client ID.
        /// </summary>
        /// <returns>Client ID.</returns>
        protected string GetWebSocketID()
        {
            return context.Request.Headers.ContainsKey("InstanceId") ?
                context.Request.Headers["InstanceId"] : string.Empty;
        }
        protected async Task UpdateMetadateEtagAsync()
        {
            int serialNumber = await fileSystemInfo.GetExtendedAttributeAsync<int?>("MetadateSerialNumber") ?? 0;

            await fileSystemInfo.SetExtendedAttributeAsync("MetadateSerialNumber", ++serialNumber);
        }
    }
}
