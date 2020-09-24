using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;

namespace CalDAVServer.SqlStorage.AspNet.CalDav
{
    public static class CalDavFactory
    {
        /// <summary>
        /// Gets CalDAV items.
        /// </summary>
        /// <param name="itemPath">Relative path requested.</param>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>Object implementing various calendar items or null if no object corresponding to path is found.</returns>
        public static async Task<IHierarchyItemAsync> GetCalDavItemAsync(DavContext context, string itemPath)
        {
            // If this is [DAVLocation]/calendars - return folder that contains all calendars.
            if (itemPath.Equals(CalendarsRootFolder.CalendarsRootFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                return new CalendarsRootFolder(context);
            }

            string[] segments = itemPath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // If URL ends with .ics - return calendar file, which contains event or to-do.
            if (itemPath.EndsWith(CalendarFile.Extension, System.StringComparison.InvariantCultureIgnoreCase))
            {
                string uid = EncodeUtil.DecodeUrlPart(Path.GetFileNameWithoutExtension(segments.Last())).Normalize(NormalizationForm.FormC);
                return (await CalendarFile.LoadByUidsAsync(context, new[] { uid }, PropsToLoad.All)).FirstOrDefault();
            }

            // If this is [DAVLocation]/calendars/[CalendarFolderId]/ return calendar.
            if (itemPath.StartsWith(CalendarsRootFolder.CalendarsRootFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                Guid calendarFolderId;
                if (Guid.TryParse(EncodeUtil.DecodeUrlPart(segments.Last()), out calendarFolderId))
                  
                {
                    return await CalendarFolder.LoadByIdAsync(context, calendarFolderId);
                }
            }

            return null;
        }
    }
}