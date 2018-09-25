using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Paging;

namespace CardDAVServer.SqlStorage.AspNet.CardDav
{
    /// <summary>
    /// Folder that contains address books.
    /// Instances of this class correspond to the following path: [DAVLocation]/addressbooks/
    /// </summary>
    public class AddressbooksRootFolder : LogicalFolder, IFolderAsync
    {
        /// <summary>
        /// This folder name.
        /// </summary>
        private static readonly string addressbooksRootFolderName = "addressbooks";

        /// <summary>
        /// Path to this folder.
        /// </summary>
        public static string AddressbooksRootFolderPath = DavLocationFolder.DavLocationFolderPath + addressbooksRootFolderName + '/';

        public AddressbooksRootFolder(DavContext context)
            : base(context, AddressbooksRootFolderPath)
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
            // Here we list addressbooks from back-end storage. 
            // You can filter addressbooks if requied and return only addressbooks that user has access to.
            return new PageResults((await AddressbookFolder.LoadAllAsync(Context)).OrderBy(x => x.Name), null);
        }

        public Task<IFileAsync> CreateFileAsync(string name)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Creates a new address book.
        /// </summary>
        /// <param name="name">Name of the new address book.</param>
        public async Task CreateFolderAsync(string name)
        {
            await AddressbookFolder.CreateAddressbookFolderAsync(Context, name, "");
        }
    }
}
