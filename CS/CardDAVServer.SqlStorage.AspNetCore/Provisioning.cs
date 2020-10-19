using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;

using ITHit.WebDAV.Server.Acl;
using CardDAVServer.SqlStorage.AspNetCore.CardDav;

namespace CardDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    public class Provisioning
    {

        /// <summary>
        /// Creates initial address books for user.
        /// </summary>
        internal static async Task CreateAddressbookFoldersAsync(DavContext context)
        {
            // If user does not have access to any address books - create new address books.
            string sql = @"SELECT ISNULL((SELECT TOP 1 1 FROM [card_Access] WHERE [UserId] = @UserId) , 0)";
            if (await context.ExecuteScalarAsync<int>(sql, "@UserId", context.UserId) < 1)
            {
                await AddressbookFolder.CreateAddressbookFolderAsync(context, "Book 1", "Address Book 1");
                await AddressbookFolder.CreateAddressbookFolderAsync(context, "Book 2", "Address Book 2");
                await AddressbookFolder.CreateAddressbookFolderAsync(context, "Book 3", "Address Book 3");
            }
        }
    }
}
