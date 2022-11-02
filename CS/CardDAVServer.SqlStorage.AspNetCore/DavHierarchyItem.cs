using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal;

using CardDAVServer.SqlStorage.AspNetCore.Acl;


namespace CardDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Base class for calendars (calendar folders), and calendar files (events and to-dos).
    /// </summary>
    public abstract class DavHierarchyItem : Discovery, IHierarchyItem, ICurrentUserPrincipal
    {
        protected string itemPath;
        protected string displayName;

        /// <summary>
        /// Gets item display name.
        /// </summary>
        public virtual string Name { get { return displayName; } }

        /// <summary>
        /// Gets item path.
        /// </summary>
        public virtual string Path { get { return itemPath; } }

        /// <summary>
        /// Gets item creation date. Must be in UTC.
        /// </summary>
        public virtual DateTime Created
        {
            get { return new DateTime(2000, 1, 1); }
        }

        /// <summary>
        /// Gets item modification date. Must be in UTC.
        /// </summary>
        public virtual DateTime Modified
        {
            get { return new DateTime(2000, 1, 1); }
        }

        public DavHierarchyItem(DavContext context) : base(context)
        {
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
            return new User(Context, Context.UserId);
        }

        public virtual async Task CopyToAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        public virtual async Task MoveToAsync(IItemCollection destFolder, string destName, MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        public abstract Task DeleteAsync(MultistatusException multistatus);

        public virtual async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> names, bool allprop)
        {
            return new PropertyValue[] { };
        }

        public virtual async Task UpdatePropertiesAsync(
            IList<PropertyValue> setProps,
            IList<PropertyName> delProps,
            MultistatusException multistatus)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            return new PropertyName[] { };
        }

    }
}
