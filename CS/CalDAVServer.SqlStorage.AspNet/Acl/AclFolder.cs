using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;


namespace CalDAVServer.SqlStorage.AspNet.Acl
{
    /// <summary>
    /// Logical folder with name 'acl' which contains folders 'users' and 'groups'.
    /// Instances of this class correspond to the following path: [DAVLocation]/acl/
    /// </summary>
    public class AclFolder : LogicalFolder
    {
        /// <summary>
        /// This folder name.
        /// </summary>
        private static readonly string aclFolderName = "acl";

        /// <summary>
        /// Path to this folder.
        /// </summary>
        public static readonly string AclFolderPath = string.Format("{0}{1}/", DavLocationFolder.DavLocationFolderPath, aclFolderName);

        /// <summary>
        /// Initializes a new instance of the AclFolder class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="name">Folder name.</param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        public AclFolder(DavContext context) : base(context, AclFolderPath)
        {
        }

        /// <summary>
        /// Retrieves children of this folder.
        /// </summary>
        /// <param name="propNames">Properties requested by client application for each child.</param>
        /// <returns>Children of this folder.</returns>
        public override async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> propNames)
        {
            // In this samle we list users folder only. Groups and groups folder is not implemented.
            return new[] {new UsersFolder(Context)};
        }
    }
}
