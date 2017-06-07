using System;
using System.IO;
using System.Web;
using System.Configuration;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;
using CalDAVServer.FileSystemStorage.AspNet.CalDav;

namespace CalDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    /// <remarks>
    /// In case of windows authentication methods in this class are using impersonation. In 
    /// case you run IIS Express and log-in as the user that is different from the one running 
    /// IIS Express, the IIS Express must run with Administrative permissions.
    /// </remarks>
    public class Provisioning: IHttpModule
    {
        /// <summary>
        /// Path to the folder which stores WebDAV files.
        /// </summary>
        private static readonly string repositoryPath = ConfigurationManager.AppSettings["RepositoryPath"] ?? string.Empty;

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

            DavContext context = new DavContext(httpContext);

            // Create calendars for the user during first log-in.
            await CreateCalendarFoldersAsync(context);
        }

        /// <summary>
        /// Creates initial calendars for user as well as inbox and outbox folders.
        /// </summary>
        internal static async Task CreateCalendarFoldersAsync(DavContext context)
        {            
            string physicalRepositoryPath = repositoryPath.StartsWith("~") ? HttpContext.Current.Server.MapPath(repositoryPath) :repositoryPath;      

            // Get path to user folder /calendars/[user_name]/ and check if it exists.
            string calendarsUserFolder = string.Format("{0}{1}", CalendarsRootFolder.CalendarsRootFolderPath.Replace('/', Path.DirectorySeparatorChar), context.UserName);
            string pathCalendarsUserFolder = Path.Combine(physicalRepositoryPath, calendarsUserFolder.TrimStart(Path.DirectorySeparatorChar));
            if (!Directory.Exists(pathCalendarsUserFolder))
            {
                Directory.CreateDirectory(pathCalendarsUserFolder);

                // Grant full control to loged-in user.
                GrantFullControl(pathCalendarsUserFolder, context);

                // Create all subfolders under the logged-in user account
                // so all folders has logged-in user as the owner.
                context.FileOperation(
                    () =>
                    {
                        // Make the loged-in user the owner of the new folder.
                        MakeOwner(pathCalendarsUserFolder, context);

                        // Create user calendars, such as /calendars/[user_name]/Calendar/.
                        string pathCalendar = Path.Combine(pathCalendarsUserFolder, "Calendar1");
                        Directory.CreateDirectory(pathCalendar);
                        pathCalendar = Path.Combine(pathCalendarsUserFolder, "Home1");
                        Directory.CreateDirectory(pathCalendar);
                        pathCalendar = Path.Combine(pathCalendarsUserFolder, "Work1");
                        Directory.CreateDirectory(pathCalendar);
                    });
            }
        }

        /// <summary>
        /// Makes the loged-in user the owner of the folder.
        /// </summary>
        /// <param name="folderPath">folder path in file system</param>
        private static void MakeOwner(string folderPath, DavContext context)
        {
            DirectorySecurity securityOwner = Directory.GetAccessControl(folderPath, AccessControlSections.Owner);
            securityOwner.SetOwner(context.WindowsIdentity.User);
            Directory.SetAccessControl(folderPath, securityOwner);
        }

        /// <summary>
        /// Grants full controll to currently loged-in user.
        /// </summary>
        /// <param name="folderPath">folder path in file system</param>
        private static void GrantFullControl(string folderPath, DavContext context)
        {
            DirectoryInfo folder = new DirectoryInfo(folderPath);
            DirectorySecurity security = folder.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(context.WindowsIdentity.User, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
            folder.SetAccessControl(security);
        }
    }
}
