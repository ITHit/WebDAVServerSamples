using System;
using System.IO;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;


namespace CardDAVServer.FileSystemStorage.AspNetCore.CardDav
{
    /// <summary>
    /// Folder that contains user folders which contain address books.
    /// Instances of this class correspond to the following path: [DAVLocation]/addressbooks/
    /// </summary>
    /// <example>
    /// [DAVLocation]
    ///  |-- ...
    ///  |-- addressbooks  -- this class
    ///      |-- [User1]
    ///      |-- ...
    ///      |-- [UserX]
    /// </example>
    public class AddressbooksRootFolder : DavFolder
    {
        /// <summary>
        /// This folder name.
        /// </summary>
        private static readonly string addressbooksRootFolderName = "addressbooks";

        /// <summary>
        /// Path to this folder.
        /// </summary>
        public static string AddressbooksRootFolderPath = string.Format("{0}{1}/", DavLocationFolder.DavLocationFolderPath, addressbooksRootFolderName);

        /// <summary>
        /// Returns address books root folder that corresponds to path or null if path does not correspond to address books root folder.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>AddressbooksRootFolder instance or null if path does not correspond to this folder.</returns>
        public static AddressbooksRootFolder GetAddressbooksRootFolder(DavContext context, string path)
        {
            if (!path.Equals(AddressbooksRootFolderPath, StringComparison.InvariantCultureIgnoreCase))
                return null;

            DirectoryInfo folder = new DirectoryInfo(context.MapPath(path));
            if (!folder.Exists)
                return null;

            return new AddressbooksRootFolder(folder, context, path);
        }

        private AddressbooksRootFolder(DirectoryInfo directory, DavContext context, string path)
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
