using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;
using CalDAVServer.SqlStorage.AspNet.CalDav;

namespace CalDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Assists in finding folders that contain calendars and address books.
    /// </summary>
    public class Discovery : ICalendarDiscoveryAsync
    {
        /// <summary>
        /// Instance of <see cref="DavContext"/>.
        /// </summary>
        protected DavContext Context;

        public Discovery(DavContext context)
        {
            this.Context = context;
        }

        /// <summary>
        /// Returns list of folders that contain calendars owned by this principal.
        /// </summary>
        /// <remarks>This enables calendars discovery owned by current loged-in principal.</remarks>
        public async Task<IEnumerable<IItemCollectionAsync>> GetCalendarHomeSetAsync()
        {
            return new[] { new CalendarsRootFolder(Context) };
        }

        /// <summary>
        /// Returns <b>true</b> if <b>calendar-home-set</b> feature is enabled, <b>false</b> otherwise.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In this method you can analyze User-Agent header to find out the client application used for accessing the server
        /// and enable/disable <b>calendar-home-set</b> feature for specific client. 
        /// </para>
        /// <para>
        /// iOS and OS X does require <b>calendar-home-set</b> feature to be always enabled. On the other hand it may consume extra 
        /// resources. Some CalDAV clients start immediate synchronization of all calendars found on the server 
        /// via home-set request. Typically you will always enable home-set for iOS and OS X CalDAV clients, but may disable it for other clients.
        /// </para>
        /// </remarks>
        public bool CalendarHomeSetEnabled
        {
            get
            {
                return true;
            }
        }
    }
}