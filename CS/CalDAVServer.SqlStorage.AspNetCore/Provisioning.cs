using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;

using ITHit.WebDAV.Server.Acl;
using CalDAVServer.SqlStorage.AspNetCore.CalDav;

namespace CalDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    public class Provisioning
    {

        /// <summary>
        /// Creates initial calendars for users.
        /// </summary>
        internal static async Task CreateCalendarFoldersAsync(DavContext context)
        {
            // If user does not have access to any calendars - create new calendars.
            string sql = @"SELECT ISNULL((SELECT TOP 1 1 FROM [cal_Access] WHERE [UserId] = @UserId) , 0)";
            if (await context.ExecuteScalarAsync<int>(sql, "@UserId", context.UserId) < 1)
            {
                await CalendarFolder.CreateCalendarFolderAsync(context, "Cal 1", "Calendar 1");
                await CalendarFolder.CreateCalendarFolderAsync(context, "Cal 2", "Calendar 2");
                await CalendarFolder.CreateCalendarFolderAsync(context, "Cal 3", "Calendar 3");
            }
        }
    }
}
