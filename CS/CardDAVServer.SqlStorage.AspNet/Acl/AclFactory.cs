using System;
using System.Linq;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;


namespace CardDAVServer.SqlStorage.AspNet.Acl
{
    /// <summary>
    /// Gets ACL principals and folders by provided path.
    /// </summary>
    internal static class AclFactory
    {
        /// <summary>
        /// Gets object representing ACL folder, user or group.
        /// </summary>
        /// <param name="path">Relative path requested.</param>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>Object implemening ACL principal or folder</returns>
        internal static async Task<IHierarchyItemAsync> GetAclItemAsync(DavContext context, string path)
        {
            // If this is [DAVLocation]/acl - return folder which contains users and groups.
            if (path.Equals(AclFolder.AclFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                return new AclFolder(context);
            }

            // If this is [DAVLocation]/acl/users - return folder which contains users.
            if (path.Equals(UsersFolder.UsersFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                return new UsersFolder(context);
            }

            // If this is [DAVLocation]/acl/users/[UserID] - return instance of User.
            if (path.StartsWith(UsersFolder.UsersFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                string userId = EncodeUtil.DecodeUrlPart(segments.Last());
                return await User.GetUserAsync(context, userId);
            }

            return null;
        }
    }
}