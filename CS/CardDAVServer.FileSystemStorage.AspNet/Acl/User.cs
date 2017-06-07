using System;
using System.Collections.Generic;
using System.Linq;
using System.DirectoryServices.AccountManagement;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Represents windows user in WebDAV hierarchy.
    /// </summary>
    public class User : PrincipalBase
    {
        internal UserPrincipal userPrincipal;

        public User(UserPrincipal userPrincipal, DavContext context) : base(userPrincipal, UserFolder.PATH, context)
        {
            if (userPrincipal == null)
            {
                throw new ArgumentNullException("userPrincipal");
            }
            this.userPrincipal = userPrincipal;
        }

        /// <summary>
        /// Creates <see cref="User"/> instance from name.
        /// </summary>
        /// <param name="name">User name.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        /// <returns>Instance of <see cref="User"/> or <c>null</c> if user is not found.</returns>
        public static User FromName(string name, DavContext context)
        {
            // Calling FindByIdentity on a machine that is not on a domain is 
            // very slow (even with new PrincipalContext(ContextType.Machine)).
            // using PrincipalSearcher on a local machine and FindByIdentity on a domain.
            Principal principal;
            PrincipalContext principalContext = context.GetPrincipalContext();

            try
            {
                if (principalContext.ContextType == ContextType.Machine)
                {
                    // search local machine
                    UserPrincipal principalToSearch = new UserPrincipal(principalContext);
                    principalToSearch.SamAccountName = name;
                    principal = new PrincipalSearcher(principalToSearch).FindOne();
                }
                else
                {
                    // search domain
                    principal = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, name);
                }
                if ((principal == null) || !(principal is UserPrincipal))
                {
                    return null;
                }
            }
            catch (PrincipalOperationException)
            {
                //This exception is thrown if user cannot be found.
                return null;
            }

            return new User(principal as UserPrincipal, context);
        }

        /// <summary>
        /// We don't implement it - users doesn't support setting members.
        /// The method is here because from WebDAV perspective there's no difference
        /// between users and groups.
        /// </summary>
        /// <param name="members">Members of the group.</param>
        public override async Task SetGroupMembersAsync(IList<IPrincipalAsync> members)
        {
            throw new DavException("User objects can not contain other users.", DavStatus.CONFLICT);
        }
        
        /// <summary>
        /// Retrieves principal members. Users have no members, so return empty list.
        /// </summary>
        /// <returns>Principal members.</returns>
        public override async Task<IEnumerable<IPrincipalAsync>> GetGroupMembersAsync()
        {
            return new IPrincipalAsync[0];
        }

        /// <summary>
        /// Checks whether this user is of well-known type.
        /// </summary>
        /// <param name="wellknownPrincipal">Type to check.</param>
        /// <returns><c>true</c> if the user is of specified well-known type.</returns>
        public override bool IsWellKnownPrincipal(WellKnownPrincipal wellknownPrincipal)
        {
            return wellknownPrincipal == WellKnownPrincipal.Unauthenticated &&
                Context.AnonymousUser != null &&
                Sid.Value == Context.AnonymousUser.User.Value;
        }

        /// <summary>
        /// Creates new user as copy of this one.
        /// </summary>
        /// <param name="destFolder">Is not used as there's no more locations a user can be copied.</param>
        /// <param name="destName">New user name.</param>
        /// <param name="deep">Whether to copy children - is not user.</param>
        /// <param name="multistatus">Is not used as there's no children.</param>
        public override async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            if (destFolder.Path != new UserFolder(Context).Path)
            {
                throw new DavException("Copying users is only allowed into the same folder", DavStatus.CONFLICT);
            }

            if (!IsValidUserName(destName))
            {
                throw new DavException("User name contains invalid characters", DavStatus.FORBIDDEN);
            }

            UserPrincipal newUser = new UserPrincipal(userPrincipal.Context)
                                  {
                                      Name = destName,
                                      Description = userPrincipal.Description
                                  };
            
            Context.PrincipalOperation(newUser.Save);
        }
        
        /// <summary>
        /// Retrieves properties of the user.
        /// </summary>
        /// <param name="props">Properties to retrieve.</param>
        /// <param name="allprop">Whether all properties shall be retrieved.</param>
        /// <returns>Property values.</returns>
        public override async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> props, bool allprop)
        {
            IEnumerable<PropertyName> propsToGet = !allprop ? props : props.Union(PrincipalProperties.ALL);

            List<PropertyValue> propValues = new List<PropertyValue>();

            foreach (PropertyName propName in propsToGet)
            {
                if (propName == PrincipalProperties.FullName)
                {
                    propValues.Add(new PropertyValue(propName, userPrincipal.DisplayName));
                }

                if (propName == PrincipalProperties.Description)
                {
                    propValues.Add(new PropertyValue(propName, userPrincipal.Description));
                }
            }

            return propValues;
        }

        /// <summary>
        /// Retrieves names of dead properties.
        /// </summary>
        /// <returns>Dead propery names.</returns>
        public override async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            return PrincipalProperties.ALL;                       
        }

        /// <summary>
        /// Updates dead properties.
        /// </summary>
        /// <param name="setProps">Properties to set.</param>
        /// <param name="delProps">Properties to delete.</param>
        /// <param name="multistatus">Here we report problems with properties.</param>
        public override async Task UpdatePropertiesAsync(IList<PropertyValue> setProps, IList<PropertyName> delProps, MultistatusException multistatus)
        {
            foreach (PropertyValue prop in setProps)
            {
                if (prop.QualifiedName == PrincipalProperties.FullName)
                {
                    userPrincipal.DisplayName = prop.Value;
                }
                else if (prop.QualifiedName == PrincipalProperties.Description)
                {
                    userPrincipal.Description = prop.Value;
                }
                else
                {
                    multistatus.AddInnerException(
                        Path,
                        prop.QualifiedName,
                        new DavException("The property was not found", DavStatus.NOT_FOUND));
                }
            }

            foreach (PropertyName p in delProps)
            {
                multistatus.AddInnerException(
                    Path,
                    p,
                    new DavException("Principal properties can not be deleted.", DavStatus.FORBIDDEN));
            }

            Context.PrincipalOperation(userPrincipal.Save);
        }

        public override string Path
        {
            get { return UserFolder.PREFIX + "/" + userPrincipal.SamAccountName; }
        }
    }
}
