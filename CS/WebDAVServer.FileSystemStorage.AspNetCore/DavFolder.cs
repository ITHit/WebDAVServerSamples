
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Quota;
using WebDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;
using ITHit.WebDAV.Server.Search;

namespace WebDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Folder in WebDAV repository.
    /// </summary>
    public class DavFolder : DavHierarchyItem, IFolderAsync, IQuotaAsync, ISearchAsync
    {

        /// <summary>
        /// Corresponding instance of <see cref="DirectoryInfo"/>.
        /// </summary>
        private readonly DirectoryInfo dirInfo;

        /// <summary>
        /// Returns folder that corresponds to path.
        /// </summary>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        /// <returns>Folder instance or null if physical folder not found in file system.</returns>
        public static async Task<DavFolder> GetFolderAsync(DavContext context, string path)
        {
            string folderPath = context.MapPath(path).TrimEnd(System.IO.Path.DirectorySeparatorChar);
            DirectoryInfo folder = new DirectoryInfo(folderPath);

            // This code blocks vulnerability when "%20" folder can be injected into path and folder.Exists returns 'true'.
            if (!folder.Exists || string.Compare(folder.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar), folderPath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return null;
            }

            return new DavFolder(folder, context, path);
        }

        /// <summary>
        /// Initializes a new instance of this class.
        /// </summary>
        /// <param name="directory">Corresponding folder in the file system.</param>
        /// <param name="context">WebDAV Context.</param>
        /// <param name="path">Encoded path relative to WebDAV root folder.</param>
        protected DavFolder(DirectoryInfo directory, DavContext context, string path)
            : base(directory, context, path.TrimEnd('/') + "/")
        {
            dirInfo = directory;
        }

        /// <summary>
        /// Called when children of this folder are being listed.
        /// </summary>
        /// <param name="propNames">List of properties to retrieve with the children. They will be queried by the engine later.</param>
        /// <returns>Children of this folder.</returns>
        public virtual async Task<IEnumerable<IHierarchyItemAsync>> GetChildrenAsync(IList<PropertyName> propNames)
        {
            // Enumerates all child files and folders.
            // You can filter children items in this implementation and 
            // return only items that you want to be visible for this 
            // particular user.

            IList<IHierarchyItemAsync> children = new List<IHierarchyItemAsync>();

            FileSystemInfo[] fileInfos = null;
            fileInfos = dirInfo.GetFileSystemInfos();

            foreach (FileSystemInfo fileInfo in fileInfos)
            {
                string childPath = Path + EncodeUtil.EncodeUrlPart(fileInfo.Name);
                IHierarchyItemAsync child = await context.GetHierarchyItemAsync(childPath);
                if (child != null)
                {
                    children.Add(child);
                }
            }

            return children;
        }

        /// <summary>
        /// Called when a new file is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new file.</param>
        /// <returns>The new file.</returns>
        public async Task<IFileAsync> CreateFileAsync(string name)
        {
            await RequireHasTokenAsync();
            string fileName = System.IO.Path.Combine(fileSystemInfo.FullName, name);

            using (FileStream stream = new FileStream(fileName, FileMode.CreateNew))
            {
            }
            await context.socketService.NotifyRefreshAsync(Path);

            return (IFileAsync)await context.GetHierarchyItemAsync(Path + EncodeUtil.EncodeUrlPart(name));
        }

        /// <summary>
        /// Called when a new folder is being created in this folder.
        /// </summary>
        /// <param name="name">Name of the new folder.</param>
        virtual public async Task CreateFolderAsync(string name)
        {
            await RequireHasTokenAsync();
            dirInfo.CreateSubdirectory(name);
            await context.socketService.NotifyRefreshAsync(Path);
        }

        /// <summary>
        /// Called when this folder is being copied.
        /// </summary>
        /// <param name="destFolder">Destination parent folder.</param>
        /// <param name="destName">New folder name.</param>
        /// <param name="deep">Whether children items shall be copied.</param>
        /// <param name="multistatus">Information about child items that failed to copy.</param>
        public override async Task CopyToAsync(IItemCollectionAsync destFolder, string destName, bool deep, MultistatusException multistatus)
        {
            DavFolder targetFolder = destFolder as DavFolder;
            if (targetFolder == null)
            {
                throw new DavException("Target folder doesn't exist", DavStatus.CONFLICT);
            }

            if (IsRecursive(targetFolder))
            {
                throw new DavException("Cannot copy to subfolder", DavStatus.FORBIDDEN);
            }

            string newDirLocalPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);

            // Create folder at the destination.
            try
            {
                if (!Directory.Exists(newDirLocalPath))
                {
                    await targetFolder.CreateFolderAsync(destName);
                }
            }
            catch (DavException ex)
            {
                // Continue, but report error to client for the target item.
                multistatus.AddInnerException(targetPath, ex);
            }

            // Copy children.
            IFolderAsync createdFolder = (IFolderAsync)await context.GetHierarchyItemAsync(targetPath);
            foreach (DavHierarchyItem item in await GetChildrenAsync(new PropertyName[0]))
            {
                if (!deep && item is DavFolder)
                {
                    continue;
                }

                try
                {
                    await item.CopyToAsync(createdFolder, item.Name, deep, multistatus);
                }
                catch (DavException ex)
                {
                    // If a child item failed to copy we continue but report error to client.
                    multistatus.AddInnerException(item.Path, ex);
                }
            }
            await context.socketService.NotifyRefreshAsync(targetFolder.Path);
        }

        /// <summary>
        /// Called when this folder is being moved or renamed.
        /// </summary>
        /// <param name="destFolder">Destination folder.</param>
        /// <param name="destName">New name of this folder.</param>
        /// <param name="multistatus">Information about child items that failed to move.</param>
        public override async Task MoveToAsync(IItemCollectionAsync destFolder, string destName, MultistatusException multistatus)
        {
            await RequireHasTokenAsync();
            DavFolder targetFolder = destFolder as DavFolder;
            if (targetFolder == null)
            {
                throw new DavException("Target folder doesn't exist", DavStatus.CONFLICT);
            }

            if (IsRecursive(targetFolder))
            {
                throw new DavException("Cannot move folder to its subtree.", DavStatus.FORBIDDEN);
            }

            string newDirPath = System.IO.Path.Combine(targetFolder.FullPath, destName);
            string targetPath = targetFolder.Path + EncodeUtil.EncodeUrlPart(destName);

            try
            {
                // Remove item with the same name at destination if it exists.
                IHierarchyItemAsync item = await context.GetHierarchyItemAsync(targetPath);
                if (item != null)
                    await item.DeleteAsync(multistatus);

                await targetFolder.CreateFolderAsync(destName);
            }
            catch (DavException ex)
            {
                // Continue the operation but report error with destination path to client.
                multistatus.AddInnerException(targetPath, ex);
                return;
            }

            // Move child items.
            bool movedSuccessfully = true;
            IFolderAsync createdFolder = (IFolderAsync)await context.GetHierarchyItemAsync(targetPath);
            foreach (DavHierarchyItem item in await GetChildrenAsync(new PropertyName[0]))
            {
                try
                {
                    await item.MoveToAsync(createdFolder, item.Name, multistatus);
                }
                catch (DavException ex)
                {
                    // Continue the operation but report error with child item to client.
                    multistatus.AddInnerException(item.Path, ex);
                    movedSuccessfully = false;
                }
            }

            if (movedSuccessfully)
            {
                await DeleteAsync(multistatus);
            }
            // Refresh client UI.
            await context.socketService.NotifyDeleteAsync(Path);
            await context.socketService.NotifyRefreshAsync(GetParentPath(targetPath));
        }

        /// <summary>
        /// Called whan this folder is being deleted.
        /// </summary>
        /// <param name="multistatus">Information about items that failed to delete.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            /*
            if (await GetParentAsync() == null)
            {
                throw new DavException("Cannot delete root.", DavStatus.NOT_ALLOWED);
            }
            */
            await RequireHasTokenAsync();
            bool allChildrenDeleted = true;
            foreach (IHierarchyItemAsync child in await GetChildrenAsync(new PropertyName[0]))
            {
                try
                {
                    await child.DeleteAsync(multistatus);
                }
                catch (DavException ex)
                {
                    //continue the operation if a child failed to delete. Tell client about it by adding to multistatus.
                    multistatus.AddInnerException(child.Path, ex);
                    allChildrenDeleted = false;
                }
            }

            if (allChildrenDeleted)
            {
                dirInfo.Delete();
                await context.socketService.NotifyDeleteAsync(Path);
            }
        }

        /// <summary>
        /// Returns free bytes available to current user.
        /// </summary>
        /// <returns>Free bytes available.</returns>
        public async Task<long> GetAvailableBytesAsync()
        {
            // Here you can return amount of bytes available for current user.
            // For the sake of simplicity we return entire available disk space.

            // Note: NTFS quotes retrieval for current user works very slowly.

            return new DriveInfo(dirInfo.Root.FullName).AvailableFreeSpace;
        }

        /// <summary>
        /// Returns used bytes by current user.
        /// </summary>
        /// <returns>Number of bytes used on disk.</returns>
        public async Task<long> GetUsedBytesAsync()
        {
            // Here you can return amount of bytes used by current user.
            // For the sake of simplicity we return entire used disk space.

            //Note: NTFS quotes retrieval for current user works very slowly.

            DriveInfo driveInfo = new DriveInfo(dirInfo.Root.FullName);
            return driveInfo.TotalSize - driveInfo.AvailableFreeSpace;
        }

        /// <summary>
        /// Searches files and folders in current folder using search phrase and options.
        /// </summary>
        /// <param name="searchString">A phrase to search.</param>
        /// <param name="options">Search options.</param>
        /// <param name="propNames">
        /// List of properties to retrieve with each item returned by this method. They will be requested by the 
        /// Engine in <see cref="IHierarchyItemAsync.GetPropertiesAsync(IList{PropertyName}, bool)"/> call.
        /// </param>
        /// <returns>List of <see cref="IHierarchyItemAsync"/> satisfying search request.</returns>
        public async Task<IEnumerable<IHierarchyItemAsync>> SearchAsync(string searchString, SearchOptions options, List<PropertyName> propNames)
        {
            // Not implemented currently. .NET Core on Mac OS X does not provide a OLEDB driver at this time.
            // To generate a Windows-specific search code use the 'ASP.NET WebDAV Server Application' wizard for Visual Studio.
            return new List<IHierarchyItemAsync>();
        }

        /// <summary>
        /// Determines whether <paramref name="destFolder"/> is inside this folder.
        /// </summary>
        /// <param name="destFolder">Folder to check.</param>
        /// <returns>Returns <c>true</c> if <paramref name="destFolder"/> is inside thid folder.</returns>
        private bool IsRecursive(DavFolder destFolder)
        {
            return destFolder.Path.StartsWith(Path);
        }
    }
}
