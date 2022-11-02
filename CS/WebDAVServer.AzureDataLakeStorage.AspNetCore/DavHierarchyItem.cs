using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class2;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// Base class for WebDAV items (folders, files, etc).
    /// </summary>
    public abstract class DavHierarchyItem : IHierarchyItem, ILock
    {

        /// <summary>
        /// Name of properties attribute.
        /// </summary>
        private const string propertiesAttributeName = "Properties";

        /// <summary>
        /// Name of locks attribute.
        /// </summary>
        private const string locksAttributeName = "Locks";
        /// <summary>
        /// Name of LastModified attribute.
        /// </summary>
        private const string lastModifiedProperty = "LastModified";

        internal const string snippetProperty = "snippet";

        /// <summary>
        /// Gets name of the item.
        /// </summary>
        public string Name => dataCloudItem.Name;

        /// <summary>
        /// Gets date when the item was created in UTC.
        /// </summary>
        public DateTime Created => dataCloudItem.CreatedUtc;

        /// <summary>
        /// Gets date when the item was last modified in UTC.
        /// </summary>
        public DateTime Modified
        {
            get
            {
                bool exists = dataCloudItem.Properties.TryGetValue(lastModifiedProperty, out string lastModified);
                if (exists)
                {
                    DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                    return start.AddMilliseconds(long.Parse(lastModified)).ToUniversalTime();
                }
                
                return dataCloudItem.ModifiedUtc;
            }
        }

        /// <summary>
        /// Gets path of the item where each part between slashes is encoded.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Corresponding DLItem.
        /// </summary>
        private readonly DataCloudItem dataCloudItem;

        /// <summary>
        /// WebDAV Context.
        /// </summary>
        protected readonly DavContext context;

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="dataCloudItem">Corresponding DLItem.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        protected DavHierarchyItem(DataCloudItem dataCloudItem, DavContext context, string path)
        {
            this.dataCloudItem = dataCloudItem;
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
        /// Deletes this item.
        /// </summary>
        /// <param name="multistatus">If some items fail to delete but operation in whole shall be continued, add
        /// information about the error into <paramref name="multistatus"/> using
        /// <see cref="MultistatusException.AddInnerException(string,ITHit.WebDAV.Server.DavException)"/>.
        /// </param>
        public abstract Task DeleteAsync(MultistatusException multistatus);

        /// <summary>
        /// Retrieves user defined property values.
        /// </summary>
        /// <param name="props">Names of dead properties which values to retrieve.</param>
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
        /// Retrieves list of user defined properties for this item.
        /// </summary>
        /// <returns>List of user defined properties.</returns>
        private async Task<List<PropertyValue>> GetPropertyValuesAsync()
        {
            return await context.DataLakeStoreService.GetExtendedAttributeAsync<List<PropertyValue>>(dataCloudItem,
                propertiesAttributeName);
        }

        /// <summary>
        /// Saves property values to extended attribute.
        /// </summary>
        /// <param name="setProps">Properties to be set.</param>
        /// <param name="delProps">Properties to be deleted.</param>
        /// <param name="multistatus">Information about properties that failed to create, update or delete.</param>
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
                        // case "Win32CreationTime":
                        //     fileSystemInfo.CreationTimeUtc = DateTime.Parse(propToSet.Value,
                        //         new System.Globalization.CultureInfo("en-US")).ToUniversalTime();
                        //     break;
                        case "Win32LastModifiedTime":
                            await UpdateLastModified(DateTime.Parse(propToSet.Value,
                                new System.Globalization.CultureInfo("en-US")).ToUniversalTime());
                            break;
                        default:
                            context.Logger.LogDebug(
                                $"Unspecified case: DavHierarchyItem.UpdateProperties {propToSet.QualifiedName.Name} from {propToSet.QualifiedName.Namespace} namespace");
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

            await context.DataLakeStoreService.SetExtendedAttributeAsync(dataCloudItem, propertiesAttributeName, propertyValues);
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));
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
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));

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
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));

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

            List<DateLockInfo> locks = await GetLocksAsync(true);
            DateLockInfo lockInfo = locks.SingleOrDefault(x => x.LockToken == lockToken);

            await RemoveExpiredLocksAsync(lockToken);

            if (lockInfo == null || lockInfo.Expiration <= DateTime.UtcNow)
            {
                throw new DavException("The lock could not be found.", DavStatus.CONFLICT);
            }
            await context.socketService.NotifyRefreshAsync(GetParentPath(Path));
        }

        /// <summary>
        /// Check that if the item is locked then client has submitted correct lock token.
        /// </summary>
        protected async Task RequireHasTokenAsync(bool skipShared = false)
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
        private async Task RequireUnlockedAsync(bool skipShared)
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
            locks = await context.DataLakeStoreService.GetExtendedAttributeAsync<List<DateLockInfo>>(dataCloudItem, locksAttributeName);
            locks?.ForEach(l => l.LockRoot = Path);
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

            await context.DataLakeStoreService.SetExtendedAttributeAsync(dataCloudItem, locksAttributeName, locks);
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

            await context.DataLakeStoreService.SetExtendedAttributeAsync(dataCloudItem, locksAttributeName, locks);
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
        /// Updates LastModified property in the custom properties.
        /// </summary>
        /// <param name="time">Time to update with.</param>
        protected async Task UpdateLastModified(DateTime time)
        {
            await context.DataLakeStoreService.SetExtendedAttributeAsync(dataCloudItem, lastModifiedProperty,
                (long) (time.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds);
        }
    }
}
