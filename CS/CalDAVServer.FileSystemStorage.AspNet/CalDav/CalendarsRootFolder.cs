using System;
using System.IO;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;


namespace CalDAVServer.FileSystemStorage.AspNet.CalDav
{
    /// <summary>
    /// Folder that contains user folders which contain calendars.
    /// Instances of this class correspond to the following path: [DAVLocation]/calendars/
    /// </summary>
    /// <example>
    /// [DAVLocation]
    ///  |-- ...
    ///  |-- calendars  -- this class
    ///      |-- [User1]
    ///      |-- ...
    ///      |-- [UserX]
    /// </example>
    public class CalendarsRootFolder : DavFolder
    {
        /// <summary>
        /// This folder name.
        /// </summary>
        private static readonly string calendarsRootFolderName = "calendars";

        /// <summary>
        /// Path to this folder.
        /// </summary>
        public static string CalendarsRootFolderPath = string.Format("{0}{1}/", DavLocationFolder.DavLocationFolderPath, calendarsRootFolderName);

        /// <summary>
        /// Returns calendars root folder that corresponds to path or null if path does not correspond to calendars root folder.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>CalendarsRootFolder instance or null if path does not correspond to this folder.</returns>
        public static CalendarsRootFolder GetCalendarsRootFolder(DavContext context, string path)
        {
            if (!path.Equals(CalendarsRootFolderPath, StringComparison.InvariantCultureIgnoreCase))
                return null;

            DirectoryInfo folder = new DirectoryInfo(context.MapPath(path));
            if (!folder.Exists)
                return null;

            return new CalendarsRootFolder(folder, context, path);
        }

        private CalendarsRootFolder(DirectoryInfo directory, DavContext context, string path)
            : base(directory, context, path)
        {
        }


        /* If required you can appy some rules, for example prohibit creating files in this folder

        /// <summary>
        /// Prohibit creating files in this folder.
        /// </summary>
        override public async Task<IFileAsync> CreateFileAsync(string name)
        {
            throw new DavException("Creating files in this folder is not implemented.", DavStatus.NOT_IMPLEMENTED);
        }

        /// <summary>
        /// Prohibit creating folders via WebDAV in this folder.
        /// </summary>
        /// <remarks>
        /// New user folders are created during first log-in.
        /// </remarks>        
        override public async Task CreateFolderAsync(string name)
        {
            throw new DavException("Creating sub-folders in this folder is not implemented.", DavStatus.NOT_IMPLEMENTED);
        }
         
        /// <summary>
        /// Prohibit copying this folder.
        /// </summary>        
        override public async Task CopyToAsync(IItemCollection destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            throw new DavException("Copying this folder is not allowed.", DavStatus.NOT_ALLOWED);
        }
        */

        /// <summary>
        /// Prohibit moving or renaming this folder
        /// </summary>        
        override public async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            throw new DavException("Moving or renaming this folder is not allowed.", DavStatus.NOT_ALLOWED);
        }

        /// <summary>
        /// Prohibit deleting this folder.
        /// </summary>        
        override public async Task DeleteAsync(MultistatusException multistatus)
        {
            throw new DavException("Deleting this folder is not allowed.", DavStatus.NOT_ALLOWED);
        }
    }
}
