using Microsoft.Win32.SafeHandles;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebDAVServer.FileSystemStorage.AspNet.ExtendedAttributes
{
    /// <summary>
    /// Provides methods for reading and writing extended attributes on files and folders on Windows.
    /// NTFS alternate data streams are used to store attributes.
    /// </summary>
    public class WindowsExtendedAttribute : IExtendedAttribute
    {
        private readonly string pathFormat = "{0}:{1}";
        private readonly int fileSystemAttributeBlockSize = 262144;
        private const int systemErrorCodeDiskFull = 0x70;

        /// <summary>
        /// Determines whether extended attributes are supported. 
        /// </summary>
        /// <param name="checkPath">File or folder path.</param>
        /// <returns>True if extended attributes are supported, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Throw when path is null or empty.</exception>
        /// <exception cref="COMException">Throw when happens some system exception.</exception>
        public async Task<bool> IsExtendedAttributesSupportedAsync(string checkPath)
        {
            if (string.IsNullOrEmpty(checkPath))
            {
                throw new ArgumentNullException("path");
            }

            checkPath = Path.GetPathRoot(checkPath);
            if (!checkPath.EndsWith("\\"))
                checkPath += "\\";

            StringBuilder volumeName = new StringBuilder(261);
            StringBuilder fileSystemName = new StringBuilder(261);
            int volSerialNumber;
            int maxFileNameLen;
            int fileSystemFlags;

            if (!GetVolumeInformation(GetWin32LongPath(checkPath), volumeName, volumeName.Capacity
                , out volSerialNumber, out maxFileNameLen, out fileSystemFlags
                , fileSystemName, fileSystemName.Capacity))
            {
                ThrowLastError();
            }

            return (fileSystemFlags & fileSystemAttributeBlockSize) == fileSystemAttributeBlockSize;
        }

        /// <summary>
        /// Checks extended attribute existence.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>True if attribute exist, false otherwise.</returns>
        public async Task<bool> HasExtendedAttributeAsync(string path, string attribName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            bool attributeExists = true;
            string fullPath = string.Format(pathFormat, GetWin32LongPath(path), attribName);

            using (SafeFileHandle safeHandler = GetSafeHandler(fullPath, FileAccess.Read, FileMode.Open, FileShare.ReadWrite))
            {
                if (safeHandler.IsInvalid)
                {
                    int lastError = Marshal.GetLastWin32Error();
                    if (lastError == 2) // File or alternate stream not found.
                    {
                        attributeExists = false;
                    }
                }
                else
                {
                    ThrowLastError();
                }
            }

            return attributeExists;
        }

        /// <summary>
        /// Gets extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        public async Task<string> GetExtendedAttributeAsync(string path, string attribName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            string fullPath = string.Format(pathFormat, GetWin32LongPath(path), attribName);

            using (SafeFileHandle safeHandler = GetSafeHandler(fullPath, FileAccess.Read, FileMode.Open, FileShare.ReadWrite))
            {
                using (FileStream fileStream = Open(safeHandler, FileAccess.Read))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream))
                    {
                        return await streamReader.ReadToEndAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        public async Task SetExtendedAttributeAsync(string path, string attribName, string attribValue)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            if (attribValue == null)
            {
                throw new ArgumentNullException("attribValue");
            }

            string fullPath = string.Format(pathFormat, GetWin32LongPath(path), attribName);

            using (SafeFileHandle safeHandler = GetSafeHandler(fullPath, FileAccess.Write, FileMode.Create, FileShare.Read))
            {
                if (safeHandler.IsInvalid)
                {
                    ThrowLastError();
                }

                using (FileStream fileStream = Open(safeHandler, FileAccess.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream))
                    {
                        await streamWriter.WriteAsync(attribValue);
                    }
                }
            }
        }

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        public async Task DeleteExtendedAttributeAsync(string path, string attribName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            string fullPath = string.Format(pathFormat, GetWin32LongPath(path), attribName);

            if (!DeleteFile(fullPath))
            {
                ThrowLastError();
            }
        }

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        public async Task DeleteExtendedAttributes(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        public async Task CopyExtendedAttributes(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        public async Task MoveExtendedAttributes(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets file handler.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="access">File access.</param>
        /// <param name="mode">Specifies how the operating system should open a file.</param>
        /// <param name="share">Sharing mode.</param>
        /// <returns>A wrapper for a file handle.</returns>
        private SafeFileHandle GetSafeHandler(string path, FileAccess access, FileMode mode, FileShare share)
        {
            if (mode == FileMode.Append)
            {
                mode = FileMode.OpenOrCreate;
            }

            int accessRights = GetRights(access);

            return CreateFile(path, accessRights, share, IntPtr.Zero, mode, 0, IntPtr.Zero);
        }

        /// <summary>
        /// Gets file stream from file handler.
        /// </summary>
        /// <param name="fileHandler">File handler.</param>
        /// <param name="access">Read, write, or read/write access to a file.</param>
        /// <returns>A file stream.</returns>
        private FileStream Open(SafeFileHandle fileHandler, FileAccess access)
        {
            if (fileHandler.IsInvalid)
            {
                ThrowLastError();
            }

            return new FileStream(fileHandler, access);
        }

        /// <summary>
        /// Gets file access rights.
        /// </summary>
        /// <param name="access">Read, write, or read/write access to a file.</param>
        /// <returns>An integer representing access rights.</returns>
        private int GetRights(FileAccess access)
        {
            switch (access)
            {
                case FileAccess.Read:
                    return int.MinValue;
                case FileAccess.Write:
                    return 1073741824;
                default:
                    return -1073741824;
            }
        }

        /// <summary>
        /// Gets long path for win32.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        private string GetWin32LongPath(string path)
        {
             if(path.StartsWith(@"\\?\"))
            {
                return path;
            }

            if(path.StartsWith(@"\\"))
            {
                return @"\\?\UNC\" + path.TrimStart('\\');
            }

            return @"\\?\" + path;
        }


        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetVolumeInformation(string lpRootPathName, StringBuilder volumeName, int volumeNameBufLen, out int volSerialNumber, out int maxFileNameLen, out int fileSystemFlags, StringBuilder fileSystemName, int fileSystemNameBufLen);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

        /// <summary>
        /// Deletes a file or a stream.
        /// </summary>
        /// <param name="path">File or stream path.</param>
        /// <returns>False if the function failed, nonzero otherwise.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string path);
  
        /// <summary>
        /// Throws last system exception.
        /// </summary>
        private static void ThrowLastError()
        {
            int lastError = Marshal.GetLastWin32Error();
            if (0 != lastError)
            {
                int hr = Marshal.GetHRForLastWin32Error();
                switch (lastError)
                {
                    case 0x20:
                        throw new IOException("Sharing violation");

                    case 2:
                        throw new FileNotFoundException();

                    case 3:
                        throw new DirectoryNotFoundException();

                    case 5:
                        throw new UnauthorizedAccessException();

                    case 15:
                        throw new DriveNotFoundException();

                    case 0x57:
                        throw new IOException();

                    case 0xb7:
                        break;

                    case 0xce:
                        throw new PathTooLongException("Path too long");

                    case 0x3e3:
                        throw new OperationCanceledException();
                }

                throw new IOException();
            }
        }
    }
}
