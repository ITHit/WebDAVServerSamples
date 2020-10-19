using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Paging;

namespace CalDAVServer.FileSystemStorage.AspNetCore.Acl
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
        /// <param name="context">Instace of <see cref="DavContext"/>.</param>
        public AclFolder(DavContext context) : base(context, aclFolderName, AclFolderPath)
        {
        }

        /// <summary>
        /// Retrieves children of /acl folder.
        /// We have here 'user' and 'group' folder for holding users and groups respectively.
        /// </summary>
        /// <param name="propNames">Property names to be fetched lated.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Children of this folder.</returns>
        public override async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            return new PageResults(new[] {new UsersFolder(Context)}, null);
        }
    }
}
