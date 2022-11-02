using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Paging;

using CardDAVServer.SqlStorage.AspNetCore.Acl;
using CardDAVServer.SqlStorage.AspNetCore.CardDav;

namespace CardDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Logical folder which contains /acl/, /calendars/ and /addressbooks/ folders.
    /// Represents a folder with the following path: [DAVLocation]
    /// </summary>
    /// <example>
    /// [DavLocation]
    ///  |-- acl
    ///  |-- calendars
    ///  |-- addressbooks
    /// </example>
    public class DavLocationFolder : LogicalFolder
    {
        /// <summary>
        /// Path to this folder.
        /// </summary>
        /// <value>Returns first non-root path from DavLocation section from config file or "/" if no DavLocation section is found.</value>
        public static string DavLocationFolderPath
        {
            get
            {
                // If no davLocation section is found or no non-root WebDAV location is specified in 
                // configuration file asume the WebDAV is on web site root.
                return "/";
            }
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        public DavLocationFolder(DavContext context)
            : base(context, DavLocationFolderPath)
        {
        }

        /// <summary>
        /// Retrieves children of this folder: /acl/, /calendars/ and /addressbooks/ folders.
        /// </summary>
        /// <param name="propNames">Properties requested by client application for each child.</param>
        /// <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        /// <param name="nResults">The number of items to return.</param>
        /// <param name="orderProps">List of order properties requested by the client.</param>
        /// <returns>Children of this folder.</returns>
        public override async Task<PageResults> GetChildrenAsync(IList<PropertyName> propNames, long? offset, long? nResults, IList<OrderProperty> orderProps)
        {
            // In this samle we list users folder only. Groups and groups folder is not implemented.
            return new PageResults(new IHierarchyItem[] 
            { 
                  new AclFolder(Context)
                , new AddressbooksRootFolder(Context)
            }, null);
        }
    }
}
