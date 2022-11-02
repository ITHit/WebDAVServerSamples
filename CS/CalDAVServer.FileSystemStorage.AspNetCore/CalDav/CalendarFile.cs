using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using ITHit.Collab;
using ITHit.Collab.Calendar;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;


namespace CalDAVServer.FileSystemStorage.AspNetCore.CalDav
{
    /// <summary>
    /// Represents a calendar file on a CalDAV server. Typically contains a single event or to-do in iCalendar format. 
    /// Instances of this class correspond to the following path: [DAVLocation]/calendars/[user_name]/[calendar_name]/[file_name].ics.
    /// </summary>
    /// <example>
    /// [DAVLocation]
    ///  |-- ...
    ///  |-- calendars
    ///      |-- ...
    ///      |-- [User2]
    ///           |-- [Calendar 1]
    ///           |-- ...
    ///           |-- [Calendar X]
    ///                |-- [File 1.ics]  -- this class
    ///                |-- ...
    ///                |-- [File X.ics]  -- this class
    /// </example>
    public class CalendarFile : DavFile, ICalendarFile
    {
        /// <summary>
        /// Returns calendar file that corresponds to path or null if no calendar file is found.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>CalendarFile instance or null if not found.</returns>
        public static CalendarFile GetCalendarFile(DavContext context, string path)
        {
            string pattern = string.Format(@"^/?{0}/(?<user_name>[^/]+)/(?<calendar_name>[^/]+)/(?<file_name>[^/]+\.ics)$",
                              CalendarsRootFolder.CalendarsRootFolderPath.Trim(new char[] { '/' }).Replace("/", "/?"));
            if (!Regex.IsMatch(path, pattern))
                return null;

            FileInfo file = new FileInfo(context.MapPath(path));
            if (!file.Exists)
                return null;

            return new CalendarFile(file, context, path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarFile"/> class.
        /// </summary>
        /// <param name="file"><see cref="FileInfo"/> for corresponding object in file system.</param>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        private CalendarFile(FileInfo file, DavContext context, string path)
            : base(file, context, path)
        {
        }

        /// <summary>
        /// Called when client application deletes this file.
        /// </summary>
        /// <param name="multistatus">Error description if case delate failed. Ignored by most clients.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            // Notify attendees that event is canceled if deletion is successful.
            string calendarObjectContent = File.ReadAllText(fileSystemInfo.FullName);

            await base.DeleteAsync(multistatus);

            IEnumerable<IComponent> calendars = new vFormatter().Deserialize(calendarObjectContent);
            ICalendar2 calendar = calendars.First() as ICalendar2;
            calendar.Method = calendar.CreateMethodProp(MethodType.Cancel);
            await iMipEventSchedulingTransport.NotifyAttendeesAsync(context, calendar);
        }

        /// <summary>
        /// Called when event or to-do is being saved to back-end storage.
        /// </summary>
        /// <param name="stream">Stream containing VCALENDAR, typically with a single VEVENT ot VTODO component.</param>
        /// <param name="contentType">Content type.</param>
        /// <param name="startIndex">Starting byte in target file
        /// for which data comes in <paramref name="content"/> stream.</param>
        /// <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        /// <returns>Whether the whole stream has been written.</returns>
        public override async Task<bool> WriteAsync(Stream content, string contentType, long startIndex, long totalFileSize)
        {
            bool result = await base.WriteAsync(content, contentType, startIndex, totalFileSize);

            // Notify attendees that event is created or modified.
            string calendarObjectContent = File.ReadAllText(fileSystemInfo.FullName);
            IEnumerable<IComponent> calendars = new vFormatter().Deserialize(calendarObjectContent);
            ICalendar2 calendar = calendars.First() as ICalendar2;
            calendar.Method = calendar.CreateMethodProp(MethodType.Request);
            await iMipEventSchedulingTransport.NotifyAttendeesAsync(context, calendar);

            return result;
        }
    }
}
