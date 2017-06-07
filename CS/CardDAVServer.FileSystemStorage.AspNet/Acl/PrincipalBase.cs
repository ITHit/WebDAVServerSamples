using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.CardDav;
using CardDAVServer.FileSystemStorage.AspNet.CardDav;
using CardDAVServer.FileSystemStorage.AspNet;
using IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipalAsync;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Base class for users and groups.
    /// </summary>
    public abstract class PrincipalBase : Discovery , IPrincipal, IAddressbookPrincipalAsync
    {
        /// <summary>
        /// Encoded path to the parent folder.
        /// </summary>
        private readonly string parentPath;

        /// <summary>
        /// Initializes a new instance of the PrincipalBase class.
        /// </summary>
        /// <param name="principal">Instance of <see cref="Principal"/>.</param>
        /// <param name="parentPath">Encoded path to parent folder.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        protected PrincipalBase(Principal principal, string parentPath, DavContext context): base(context)
        {
            this.Principal = principal;
            this.parentPath = parentPath;
            this.Context = context;
        }

        /// <summary>
        /// Gets corresponding <see cref="Principal"/>.
        /// </summary>
        public Principal Principal { get; private set; }

        /// <summary>
        /// Gets principal name.
        /// </summary>
        public string Name
        {
            get { return Principal.SamAccountName; }
        }

        /// <summary>
        /// Gets date when principal was created.
        /// </summary>
        public DateTime Created
        {
            get
            { 
                object o = ((DirectoryEntry)Principal.GetUnderlyingObject()).Properties["whenCreated"].Value;
                return o != null ? (DateTime)o : new DateTime(2000, 1, 1).ToUniversalTime();
            }
        }

        /// <summary>
        /// Gets date when principal was modified.
        /// </summary>
        public DateTime Modified
        {
            get
            {
                object o = ((DirectoryEntry)Principal.GetUnderlyingObject()).Properties["whenChanged"].Value;
                return o != null ? (DateTime)o : new DateTime(2000, 1, 1).ToUniversalTime();
            }
        }

        /// <summary>
        /// Gets principal's security identifier.
        /// </summary>
        public SecurityIdentifier Sid
        {
            get { return Principal.Sid; }
        }

        /// <summary>
        /// Gets encoded path to this principal.
        /// </summary>
        public abstract string Path { get; }

        /// <summary>
        /// Gets instance of <see cref="DavContext"/>.
        /// </summary>
        protected DavContext Context { get; private set; }

        /// <summary>
        /// Checks principal name for validity.
        /// </summary>
        /// <param name="name">Name to check.</param>
        /// <returns>Whether principal name is valid.</returns>
        public static bool IsValidUserName(string name)
        {
            char[] invChars = new[] { '"', '/', '\\', '[', ']', ':', ';', '|', '=', ',', '+', '*', '?', '<', '>' };
            return !invChars.Where(c => name.Contains(c)).Any();
        }

        /// <summary>
        /// Gets groups to which this principal belongs.
        /// </summary>
        /// <returns>Enumerable with groups.</returns>
        public async Task<IEnumerable<IPrincipal>> GetGroupMembershipAsync()
        {
            return Principal.GetGroups().Select(group => (IPrincipal)new Group((GroupPrincipal)group, Context));
        }

        /// <summary>
        /// Deletes the principal.
        /// </summary>
        /// <param name="multistatus">We don't use it currently as there are no child objects.</param>
        public async Task DeleteAsync(MultistatusException multistatus)
        {
            Context.PrincipalOperation(Principal.Delete);
        }

        /// <summary>
        /// Renames principal.
        /// </summary>
        /// <param name="destFolder">We don't use it as moving groups to different folder is not supported.</param>
        /// <param name="destName">New name.</param>
        /// <param name="multistatus">We don't use it as there're no child objects.</param>
        public async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            if (destFolder.Path != parentPath)
            {
                throw new DavException("Moving principals is only allowed into the same folder", DavStatus.CONFLICT);
            }

            if (!IsValidUserName(destName))
            {
                throw new DavException("Principal name contains invalid characters", DavStatus.FORBIDDEN);
            }

            Context.PrincipalOperation(
                () => ((DirectoryEntry)Principal.GetUnderlyingObject()).Rename(destName));
        }

        public abstract Task SetGroupMembersAsync(IList<IPrincipal> members);

        public abstract Task<IEnumerable<IPrincipal>> GetGroupMembersAsync();

        public abstract bool IsWellKnownPrincipal(WellKnownPrincipal wellknownPrincipal);

        public abstract Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> props, bool allprop);

        public abstract Task<IEnumerable<PropertyName>> GetPropertyNamesAsync();

        public abstract Task UpdatePropertiesAsync(
            IList<PropertyValue> setProps,
            IList<PropertyName> delProps,
            MultistatusException multistatus);

        public abstract Task CopyToAsync(
            IItemCollectionAsync destFolder,
            string destName,
            bool deep,
            MultistatusException multistatus);
    }
}
