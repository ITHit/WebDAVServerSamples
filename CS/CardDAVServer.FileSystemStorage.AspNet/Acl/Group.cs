using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipalAsync;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Item which represents user group.
    /// These items are located under '/acl/group/' folder.
    /// </summary>
    public class Group : PrincipalBase
    {
        /// <summary>
        /// Corresponding .net GroupPrincipal object.
        /// </summary>
        private readonly GroupPrincipal groupPrincipal;

        /// <summary>
        /// Initializes a new instance of the Group class.
        /// </summary>
        /// <param name="groupPrincipal">Corresponding .net GroupPrincipal object.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        public Group(GroupPrincipal groupPrincipal, DavContext context) : 
            base(groupPrincipal, GroupFolder.PATH, context)
        {
            this.groupPrincipal = groupPrincipal;
        }

        /// <summary>
        /// Group path.
        /// </summary>
        public override string Path
        {
            get { return GroupFolder.PREFIX + "/" + EncodeUtil.EncodeUrlPart(Name); }
        }

        /// <summary>
        /// Creates group item from name.
        /// </summary>
        /// <param name="name">Group name.</param>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <returns><see cref="Group"/> instance.</returns>
        public static Group FromName(string name, DavContext context)
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
                    GroupPrincipal principalToSearch = new GroupPrincipal(principalContext);
                    principalToSearch.SamAccountName = name;
                    principal = new PrincipalSearcher(principalToSearch).FindOne();
                }
                else
                {
                    // search domain
                    principal = GroupPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, name);
                }
                if ((principal == null) || !(principal is GroupPrincipal))
                {
                    return null;
                }
            }
            catch (PrincipalOperationException)
            {
                //This exception is thrown if user cannot be found.
                return null;
            }

            return new Group(principal as GroupPrincipal, context);
        }

        /// <summary>
        /// Sets members of the group.
        /// </summary>
        /// <param name="newMembers">List of group members.</param>
        public override async Task SetGroupMembersAsync(IList<IPrincipal> newMembers)
        {
            PrincipalCollection members = groupPrincipal.Members;
            IEnumerable<Principal> toDelete = members.Where(m => !newMembers.Where(nm => ((PrincipalBase)nm).Principal.Sid.Value == m.Sid.Value).Any()).ToList();
            foreach (Principal p in toDelete)
            {
                groupPrincipal.Members.Remove(p);
            }

            IEnumerable<IPrincipal> toAdd = newMembers.Where(nm => !members.Where(m => m.Sid.Value == ((PrincipalBase)nm).Principal.Sid.Value).Any()).ToList();
            foreach (PrincipalBase p in toAdd)
            {
                groupPrincipal.Members.Add(p.Principal);
            }

            Context.PrincipalOperation(groupPrincipal.Save);
        }

        /// <summary>
        /// Retrieves members of the group.
        /// </summary>
        /// <returns>Enumerable of group members.</returns>
        public override async Task<IEnumerable<IPrincipal>> GetGroupMembersAsync()
        {
            return groupPrincipal.GetMembers().Select(convertPrincipal);
        }

        /// <summary>
        /// Determines whether this is a wellknown group of <paramref name="wellKnown"/> type.
        /// </summary>
        /// <param name="wellKnown">Type of wellknown pricipal to check.</param>
        /// <returns><c>true</c> if the group is of questioned wellknown type.</returns>
        public override bool IsWellKnownPrincipal(WellKnownPrincipal wellKnown)
        {
            switch (wellKnown)
            {
                case WellKnownPrincipal.All:
                    return groupPrincipal.Sid.IsWellKnown(WellKnownSidType.WorldSid);
                case WellKnownPrincipal.Authenticated:
                    return groupPrincipal.Sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid);
            }

            return false;
        }

        /// <summary>
        /// Copies this item into <paramref name="destFolder"/> and renames it to <paramref name="destName"/>.
        /// </summary>
        /// <param name="destFolder">Folder to copy this item to.</param>
        /// <param name="destName">New name of the item.</param>
        /// <param name="deep">Whether child objects shall be copied.</param>
        /// <param name="multistatus">Multistatus to populate with errors.</param>
        public override async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            if (destFolder.Path != new GroupFolder(Context).Path)
            {
                throw new DavException("Copying groups is only allowed into the same folder", DavStatus.CONFLICT);
            }

            if (!IsValidUserName(destName))
            {
                throw new DavException("User name contains invalid characters", DavStatus.FORBIDDEN);
            }

            GroupPrincipal newGroup = new GroupPrincipal(groupPrincipal.Context)
                                   {
                                       Name = destName,
                                       Description = groupPrincipal.Description
                                   };
            
            Context.PrincipalOperation(newGroup.Save);
        }
       
        /// <summary>
        /// Retrieves dead properties of the group.
        /// </summary>
        /// <param name="props">Properties to retrieve.</param>
        /// <param name="allprop">Whether all properties shall be retrieved.</param>
        /// <returns>Values of requested properties.</returns>
        public override async Task<IEnumerable<PropertyValue>> GetPropertiesAsync(IList<PropertyName> props, bool allprop)
        {
            IEnumerable<PropertyName> propsToGet = !allprop ? props : props.Union(new[] { PrincipalProperties.Description });

            List<PropertyValue> propList = new List<PropertyValue>();

            foreach (PropertyName propName in propsToGet)
            {
                //we support only description.
                if (propName == PrincipalProperties.Description)
                {
                    propList.Add(new PropertyValue(propName, groupPrincipal.Description));
                }
            }

            return propList;
        }

        /// <summary>
        /// Names of dead properties. Only description is supported for windows groups.
        /// </summary>
        /// <returns>List of dead property names.</returns>
        public override async Task<IEnumerable<PropertyName>> GetPropertyNamesAsync()
        {
            return new[] { PrincipalProperties.Description };
        }

        /// <summary>
        /// Update dead properties.
        /// Currently on description is supported.
        /// </summary>
        /// <param name="setProps">Dead properties to be set.</param>
        /// <param name="delProps">Dead properties to be deleted.</param>
        /// <param name="multistatus">Multistatus with details for every failed property.</param>
        public override async Task UpdatePropertiesAsync(IList<PropertyValue> setProps, IList<PropertyName> delProps, MultistatusException multistatus)
        {        
            foreach (PropertyValue prop in setProps)
            {
                if (prop.QualifiedName == PrincipalProperties.Description)
                {
                    groupPrincipal.Description = prop.Value;
                }
                else
                {
                    //Property is not supported. Tell client about this.
                    multistatus.AddInnerException(
                        Path,
                        prop.QualifiedName,
                        new DavException("Property was not found.", DavStatus.NOT_FOUND));
                }
            }
            //We cannot delete any properties from group, because it is windows object.
            foreach (PropertyName propertyName in delProps)
            {
                multistatus.AddInnerException(
                    Path,
                    propertyName,
                    new DavException(
                        "It is not allowed to remove properties on principal objects.",
                         DavStatus.FORBIDDEN));
            }
            
            Context.PrincipalOperation(groupPrincipal.Save);
        }
      
        /// <summary>
        /// Converts .net <see cref="Principal"/> to either <see cref="Group"/> or <see cref="User"/>.
        /// </summary>
        /// <param name="principal">Principal to convert.</param>
        /// <returns>Either user or group.</returns>
        private IPrincipal convertPrincipal(Principal principal)
        {
            if (principal is UserPrincipal)
            {
                return new User((UserPrincipal)principal, Context);
            }

            return new Group((GroupPrincipal)principal, Context);
        }
    }
}
