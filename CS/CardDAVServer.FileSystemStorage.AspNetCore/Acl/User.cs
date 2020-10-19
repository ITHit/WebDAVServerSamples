using System;
using System.Linq;
using System.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.CardDav;

namespace CardDAVServer.FileSystemStorage.AspNetCore.Acl
{
    /// <summary>
    /// This class represents user principal in WebDAV hierarchy. 
    /// Instances of this class correspond to the following path: [DAVLocation]/acl/users/[UserID].
    /// </summary>
    public class User : Discovery, IAddressbookPrincipalAsync
    {
        private readonly string email;

        public static async Task<User> GetUserAsync(DavContext context, string userId)
        {
            DavUser user = context.Users.FirstOrDefault(p => p.UserName.Equals(userId, StringComparison.InvariantCultureIgnoreCase));

            return new User(context, userId, user.UserName, user.Email, new DateTime(2000, 1, 1), new DateTime(2000, 1, 1));
        }

        /// <summary>
        /// Creates instance of User class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="userId">ID of this user</param>
        /// <remarks>
        /// This consturctor is called when user URL is required, typically when discovering user calendars, 
        /// no need to populate all properties, only user ID is required.
        /// </remarks>
        public User(DavContext context, string userId)
            : this(context, userId, null, null, new DateTime(2000, 1, 1), new DateTime(2000, 1, 1))
        {
        }


        /// <summary>
        /// Creates instance of User class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="userId">ID of this user</param>
        /// <param name="name">User name.</param>
        /// <param name="email">User e-mail.</param>
        /// <param name="created">Date when this item was created.</param>
        /// <param name="modified">Date when this item was modified.</param>
        public User(DavContext context, string userId, string name, string email, DateTime created, DateTime modified)
            : base(context)
        {
            this.Name = name;
            this.email = email;
            this.Path = UsersFolder.UsersFolderPath + EncodeUtil.EncodeUrlPart(userId);
            this.Created = created;
            this.Modified = modified;
        }

        /// <summary>
        /// Gets principal name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets encoded path to this principal.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Gets date when principal was created.
        /// </summary>
        public DateTime Created { get; private set; }

        /// <summary>
        /// Gets date when principal was modified.
        /// </summary>
        public DateTime Modified { get; private set; }

        /// <summary>
        /// Creates new user as copy of this one.
        /// </summary>
        /// <param name="destFolder">Is not used as there's no more locations a user can be copied.</param>
        /// <param name="destName">New user name.</param>
        /// <param name="deep">Whether to copy children - is not user.</param>
        /// <param name="multistatus">Is not used as there's no children.</param>
        public async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Renames principal.
        /// </summary>
        /// <param name="destFolder">We don't use it as moving groups to different folder is not supported.</param>
        /// <param name="destName">New name.</param>
        /// <param name="multistatus">We don't use it as there're no child objects.</param>
        public async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Deletes the principal.
        /// </summary>
        /// <param name="multistatus">We don't use it currently as there are no child objects.</param>
        public async Task DeleteAsync(MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Retrieves properties of the user.
        /// </summary>
        /// <param name="props">Properties to retrieve.</param>
        /// <param name="allprop">Whether all properties shall be retrieved.</param>
        /// <returns>Property values.</returns>
        public async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> props, bool allprop)
        {
            return new PropertyValue[0];
        }

        /// <summary>
        /// Retrieves names of dead properties.
        /// </summary>
        /// <returns>Dead propery names.</returns>
        public async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            return new PropertyName[0];
        }

        /// <summary>
        /// Updates dead properties.
        /// </summary>
        /// <param name="setProps">Properties to set.</param>
        /// <param name="delProps">Properties to delete.</param>
        /// <param name="multistatus">Here we report problems with properties.</param>
        public async Task UpdatePropertiesAsync(IList<PropertyValue> setProps, IList<PropertyName> delProps, MultistatusException multistatus)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// We don't implement it - users doesn't support setting members.
        /// The method is here because from WebDAV perspective there's no difference
        /// between users and groups.
        /// </summary>
        /// <param name="members">Members of the group.</param>
        public async Task SetGroupMembersAsync(IList<IPrincipalAsync> members)
        {
            throw new DavException("User objects can not contain other users.", DavStatus.CONFLICT);
        }

        /// <summary>
        /// Retrieves principal members. Users have no members, so return empty list.
        /// </summary>
        /// <returns>Principal members.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> GetGroupMembersAsync()
        {
            return new IPrincipalAsync[0];
        }

        /// <summary>
        /// Gets groups to which this principal belongs.
        /// </summary>
        /// <returns>Enumerable with groups.</returns>
        public async Task<IEnumerable<IPrincipalAsync>> GetGroupMembershipAsync()
        {
            return new IPrincipalAsync[0];
        }

        /// <summary>
        /// Checks whether this user is of well-known type.
        /// </summary>
        /// <param name="wellknownPrincipal">Type to check.</param>
        /// <returns><c>true</c> if the user is of specified well-known type.</returns>
        public bool IsWellKnownPrincipal(WellKnownPrincipal wellknownPrincipal)
        {
            return (wellknownPrincipal == WellKnownPrincipal.Unauthenticated) && (Name == "Anonymous");
        }
    }
}