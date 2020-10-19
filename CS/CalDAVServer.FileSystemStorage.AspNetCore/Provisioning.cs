using System;
using System.IO;
using System.Threading.Tasks;
using CalDAVServer.FileSystemStorage.AspNetCore.CalDav;

namespace CalDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    /// <remarks>
    /// In case of windows authentication methods in this class are using impersonation. In 
    /// case you run IIS Express and log-in as the user that is different from the one running 
    /// IIS Express, the IIS Express must run with Administrative permissions.
    /// </remarks>
    public class Provisioning
    {

        /// <summary>
        /// Creates initial calendars for user as well as inbox and outbox folders.
        /// </summary>
        internal static async Task CreateCalendarFoldersAsync(DavContext context)
        {
            string physicalRepositoryPath = context.RepositoryPath;

            // Get path to user folder /calendars/[user_name]/ and check if it exists.
            string calendarsUserFolder = string.Format("{0}{1}", CalendarsRootFolder.CalendarsRootFolderPath.Replace('/', Path.DirectorySeparatorChar), context.UserName);
            string pathCalendarsUserFolder = Path.Combine(physicalRepositoryPath, calendarsUserFolder.TrimStart(Path.DirectorySeparatorChar));
            if (!Directory.Exists(pathCalendarsUserFolder))
            {
                Directory.CreateDirectory(pathCalendarsUserFolder);

                        // Create user calendars, such as /calendars/[user_name]/Calendar/.
                        string pathCalendar = Path.Combine(pathCalendarsUserFolder, "Calendar1");
                        Directory.CreateDirectory(pathCalendar);
                        pathCalendar = Path.Combine(pathCalendarsUserFolder, "Home1");
                        Directory.CreateDirectory(pathCalendar);
                        pathCalendar = Path.Combine(pathCalendarsUserFolder, "Work1");
                        Directory.CreateDirectory(pathCalendar);
            }
        }
    }
}
