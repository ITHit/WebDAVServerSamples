using System;
using System.Configuration;
using System.IO;
using System.Web;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;

using ITHit.WebDAV.Server.Acl;
using CalDAVServer.SqlStorage.AspNet.CalDav;

namespace CalDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    public class Provisioning: IHttpModule
    {

        public void Dispose()
        {
        }

        public void Init(HttpApplication application)
        {
            EventHandlerTaskAsyncHelper postAuthAsyncHelper = new EventHandlerTaskAsyncHelper(App_OnPostAuthenticateRequestAsync);
            application.AddOnPostAuthenticateRequestAsync(postAuthAsyncHelper.BeginEventHandler, postAuthAsyncHelper.EndEventHandler);
        }

        private async Task App_OnPostAuthenticateRequestAsync(object source, EventArgs eventArgs)
        {
            HttpContext httpContext = HttpContext.Current;
            if ((httpContext.User == null) || !httpContext.User.Identity.IsAuthenticated)
                return;

            using (DavContext context = new DavContext(httpContext))
            {
                // Create calendars for the user during first log-in.
                await CreateCalendarFoldersAsync(context);

                // Closes transaction. Calls ContextAsync{IHierarchyItemAsync}.BeforeResponseAsync only first time this method is invoked.
                // This method must be called manually if ContextAsync{IHierarchyItemAsync} is used outside of DavEngine. 
                await context.EnsureBeforeResponseWasCalledAsync();
            }
        }

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
