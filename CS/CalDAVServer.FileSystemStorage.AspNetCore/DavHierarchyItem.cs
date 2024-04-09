using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal;
using ITHit.WebDAV.Server.Extensibility;

using CalDAVServer.FileSystemStorage.AspNetCore.Acl;
using CalDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;

namespace CalDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Base class for WebDAV items (folders, files, etc).
    /// </summary>
    public abstract class DavHierarchyItem : Discovery , IHierarchyItem, ICurrentUserPrincipal
    {

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
        protected DavHierarchyItem(FileSystemInfo fileSystemInfo, DavContext context, string path): base(context)
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
        }

        /// <summary>
        /// Returns instance of <see cref="IPrincipal"/> which represents current user.
        /// </summary>
        /// <returns>Current user.</returns>
        /// <remarks>
        /// This method is usually called by the Engine when CalDAV/CardDAV client 
        /// is trying to discover current user URL.
        /// </remarks>
        public async Task<IPrincipal> GetCurrentUserPrincipalAsync()
        {
            // Typically there is no need to load all user properties here, only current 
            // user ID (or name) is required to form the user URL: [DAVLocation]/acl/users/[UserID]
            return new User(context, context.UserName);
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
    }
}
