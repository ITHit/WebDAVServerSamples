using System.Web.Hosting;
using System.DirectoryServices.AccountManagement;
using System.Text;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;

namespace CardDAVServer.FileSystemStorage.AspNet.Acl
{
    internal static class AclFactory
    {
        /// <summary>
        /// Gets object representing ACL folder/user/group.
        /// </summary>
        /// <param name="path">Relative path requested.</param>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>Object implemening LogicalFolder/IPrincipalFolder</returns>
        internal static async Task<IHierarchyItemAsync> GetAclItemAsync(DavContext context, string path)
        {
            //If this is /acl - return fake folder which contains users and groups.
            if (path == AclFolder.PREFIX)
            {
                return new AclFolder(context);
            }

            //if this is /acl/users - return fake folder which contains users.
            if (path == UserFolder.PREFIX)
            {
                return new UserFolder(context);
            }

            //if this is /acl/groups - return fake folder which contains groups.
            if (path == GroupFolder.PREFIX)
            {
                return new GroupFolder(context);
            }

            //if this is /acl/users/<user name> - return instance of User.
            if (path.StartsWith(UserFolder.PATH))
            {
                string name = EncodeUtil.DecodeUrlPart(path.Substring(UserFolder.PATH.Length)).Normalize(NormalizationForm.FormC);
                //we don't need an exception here - so check for validity.
                if (PrincipalBase.IsValidUserName(name))
                {
                    return User.FromName(name, context);
                }
            }

            //if this is /acl/groups/<group name> - return instance of Group.
            if (path.StartsWith(GroupFolder.PATH))
            {
                string name = EncodeUtil.DecodeUrlPart(path.Substring(GroupFolder.PATH.Length)).Normalize(NormalizationForm.FormC);
                if (PrincipalBase.IsValidUserName(name))
                {
                    return Group.FromName(name, context);
                }
            }
            return null;
        }

        /// <summary>
        /// Creates <see cref="Group"/> or <see cref="User"/> for windows SID.
        /// </summary>
        /// <param name="sid">Windows SID.</param>
        /// <param name="context">Instance of <see cref="DavContext"/>.</param>
        /// <returns>Corresponding <see cref="User"/> or <see cref="Group"/> or <c>null</c> if there's no user
        /// or group which correspond to specified sid.</returns>
        public static IPrincipalAsync GetPrincipalFromSid(string sid, DavContext context)
        {
            using (HostingEnvironment.Impersonate())
            { // This code runs as the application pool user
                Principal pr = Principal.FindByIdentity(context.GetPrincipalContext(), IdentityType.Sid, sid);

                if (pr == null)
                    return null;

                return pr is GroupPrincipal
                           ? (IPrincipalAsync)new Group((GroupPrincipal)pr, context)
                           : new User((UserPrincipal)pr, context);
            }
        }
    }
}