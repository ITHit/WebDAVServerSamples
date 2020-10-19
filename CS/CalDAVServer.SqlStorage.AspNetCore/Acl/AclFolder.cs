using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Paging;

namespace CalDAVServer.SqlStorage.AspNetCore.Acl
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
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Children of this folder.</returns>
        public override async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            // In this samle we list users folder only. Groups and groups folder is not implemented.
            return new PageResults(new[] {new UsersFolder(Context)}, null);
        }
    }
}
