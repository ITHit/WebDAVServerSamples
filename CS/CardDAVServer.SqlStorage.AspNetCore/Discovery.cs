using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;
using CardDAVServer.SqlStorage.AspNetCore.CardDav;

namespace CardDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Assists in finding folders that contain calendars and address books.
    /// </summary>
    public class Discovery : IAddressbookDiscovery
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
        /// Returns list of folders that contain address books owned by this principal.
        /// </summary>
        /// <remarks>This enables address books discovery owned by current loged-in principal.</remarks>
        public async Task<IEnumerable<IItemCollection>> GetAddressbookHomeSetAsync()
        {
            return new[] { new AddressbooksRootFolder(Context) };
        }

        /// <summary>
        /// Returns <b>true</b> if <b>addressbook-home-set</b> feature is enabled, <b>false</b> otherwise.
        /// </summary>
        /// <remarks>
        /// <para>
        /// In this method you can analyze User-Agent header and enable/disable <b>addressbook-home-set</b> feature for specific client. 
        /// </para>
        /// <para>
        /// iOS and OS X does require <b>addressbook-home-set</b> feature to be always enabled. On the other hand it may consume extra 
        /// resources. Some CardDAV clients starts immediate synchronization of all address books found on the server 
        /// via home-set request. Typically you will always enable heome-set for iOS and OS X CardDAV clients, but may disable it for other clients.
        /// </para>
        /// </remarks>
        public bool AddressbookHomeSetEnabled
        {
            get
            {
                return true;
            }
        }
    }
}