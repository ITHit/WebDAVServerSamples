using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;

namespace CalDAVServer.FileSystemStorage.AspNet.Acl
{
    /// <summary>
    /// Logical folder right under webdav root with name 'acl' which
    /// contains folders 'users' and 'groups'.
    /// </summary>
    public class AclFolder : LogicalFolder
    {
        /// <summary>
        /// Path to current logical folder which contains users and groups.
        /// </summary>
        public static string PREFIX = "acl";

        /// <summary>
        /// Gets path to current logical folder which contains users and groups.
        /// </summary>
        private static string PATH
        {
            get { return PREFIX + "/"; }
        }
        /// <summary>
        /// Initializes a new instance of the AclFolder class.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContext"/>.</param>
        public AclFolder(DavContext context) : base(context, "acl", PATH)
        {
        }

        /// <summary>
        /// Retrieves children of /acl folder.
        /// We have here 'user' and 'group' folder for holding users and groups respectively.
        /// </summary>
        /// <param name="propNames">Property names to be fetched lated.</param>
        /// <returns>Children of this folder.</returns>
        public override async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> propNames)
        {
            IList<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();
            children.Add(new UserFolder(Context));
            children.Add(new GroupFolder(Context));
            return children;
        }
    }
}
