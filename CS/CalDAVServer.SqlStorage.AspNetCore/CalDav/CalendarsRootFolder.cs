using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Paging;

namespace CalDAVServer.SqlStorage.AspNetCore.CalDav
{
    /// <summary>
    /// Folder that contains calendars.
    /// Instances of this class correspond to the following path: [DAVLocation]/calendars/
    /// </summary>
    public class CalendarsRootFolder : LogicalFolder, IFolderAsync
    {
        /// <summary>
        /// This folder name.
        /// </summary>
        private static readonly string calendarsRootFolderName = "calendars";

        /// <summary>
        /// Path to this folder.
        /// </summary>
        public static string CalendarsRootFolderPath = DavLocationFolder.DavLocationFolderPath + calendarsRootFolderName + '/';

        public CalendarsRootFolder(DavContext context)
            : base(context, CalendarsRootFolderPath)
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
            // Here we list calendars from back-end storage. 
            // You can filter calendars if requied and return only calendars that user has access to.
            return new PageResults((await CalendarFolder.LoadAllAsync(Context)).OrderBy(x => x.Name), null);
        }

        public Task<IFileAsync> CreateFileAsync(string name)
        {
            throw new DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Creates a new calendar.
        /// </summary>
        /// <param name="name">Name of the new calendar.</param>
        public async Task CreateFolderAsync(string name)
        {
            await CalendarFolder.CreateCalendarFolderAsync(Context, name, "");
        }
    }
}
