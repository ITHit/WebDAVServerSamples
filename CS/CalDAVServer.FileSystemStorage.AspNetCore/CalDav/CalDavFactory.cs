using ITHit.WebDAV.Server;


namespace CalDAVServer.FileSystemStorage.AspNetCore.CalDav
{
    /// <summary>
    /// Represents a factory for creating CalDAV items.
    /// </summary>
    public static class CalDavFactory
    {
        /// <summary>
        /// Gets CalDAV items.
        /// </summary>
        /// <param name="path">Relative path requested.</param>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>Object implementing various CalDAV items or null if no object corresponding to path is found.</returns>
        internal static IHierarchyItemAsync GetCalDavItem(DavContext context, string path)
        {
            IHierarchyItemAsync item = null;

            item = CalendarsRootFolder.GetCalendarsRootFolder(context, path);
            if (item != null)
                return item;

            item = CalendarFolder.GetCalendarFolder(context, path);
            if (item != null)
                return item;

            item = CalendarFile.GetCalendarFile(context, path);
            if (item != null)
                return item;

            return null;
        }
    }
}