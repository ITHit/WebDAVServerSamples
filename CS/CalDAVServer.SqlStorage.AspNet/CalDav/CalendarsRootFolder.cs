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



namespace CalDAVServer.SqlStorage.AspNet.CalDav
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
        /// <returns>Children of this folder.</returns>
        public override async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> propNames)
        {           
            // Here we list calendars from back-end storage. 
            // You can filter calendars if requied and return only calendars that user has access to.
            return (await CalendarFolder.LoadAllAsync(Context)).OrderBy(x => x.Name);
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
