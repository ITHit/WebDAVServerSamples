using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;
using CardDAVServer.FileSystemStorage.AspNetCore.CardDav;


namespace CardDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Assists in finding folders that contain calendars and address books.
    /// </summary>
    public class Discovery : IAddressbookDiscovery
    {
        /// <summary>
        /// Instance of <see cref="DavContext"/>.
        /// </summary>
        private DavContext context;

        public Discovery(DavContext context)
        {
            this.context = context;
        }

        /// <summary>
        /// Returns list of folders that contain address books owned by this principal.
        /// </summary>
        /// <remarks>This enables address books discovery owned by current loged-in principal.</remarks>
        public async Task<IEnumerable<IItemCollection>> GetAddressbookHomeSetAsync()
        {
            string addressbooksUserFolder = string.Format("{0}{1}/", AddressbooksRootFolder.AddressbooksRootFolderPath, context.UserName);
            return new[] { await DavFolder.GetFolderAsync(context, addressbooksUserFolder) };
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
        /// resources especially with iOS CardDAV client. iOS starts immediate synchronization of all address books found on the server 
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