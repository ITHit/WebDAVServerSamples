using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebDAVServer.FileSystemStorage.AspNetCore.Cookies.ExtendedAttributes
{
    /// <summary>
    /// Provides methods for reading and writing extended attributes on files and folders on Mac OS X.
    /// </summary>
    public class OSXExtendedAttribute : IExtendedAttribute
    {
        /// <summary>
        /// Errno for not existing attribute.
        /// </summary>
        private const int AttributeNotFoundErrno = 93;

        /// <summary>
        /// Max size for error message buffer.
        /// </summary>
        private const int ErrorMessageBufferMaxSize = 255;

        /// <summary>
        /// Dynamic C library name.
        /// </summary>
        private const string libCName = "libSystem.dylib";

        /// <summary>
        /// Determines whether extended attributes are supported.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <returns>True if extended attributes are supported, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Throw when path is null or empty.</exception>
        public async Task<bool> IsExtendedAttributesSupportedAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            long attributeCount = ListXAattr(path, new StringBuilder(), 0, 0);
            return attributeCount >= 0;
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

            long attributeSize = GetXAttr(path, attribName, new byte[0], 0, 0, 0);
            bool attributeExists = true;

            if (attributeSize < 0)
            {
                if (Marshal.GetLastWin32Error() == AttributeNotFoundErrno)
                {
                    attributeExists = false;
                }
            }

            return attributeExists;
        }

        /// <summary>
        /// Reads extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        /// <exception cref="ArgumentNullException">Throw when path is null or empty or attribName is null or empty.</exception>
        /// <exception cref="IOException">Throw when file or attribute is no available.</exception>
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

            long attributeSize = GetXAttr(path, attribName, new byte[0], 0, 0, 0);
            byte[] buffer = new byte[attributeSize];
            long readedLength = GetXAttr(path, attribName, buffer, attributeSize, 0, 0);

            if (readedLength == -1)
            {
                ThrowLastException(path, attribName);
            }

            string attributeValue = Encoding.UTF8.GetString(buffer);

            return attributeValue;
        }

        /// <summary>
        /// Writes extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        /// <exception cref="ArgumentNullException">Throw when path is null or empty or attribName is null or empty.</exception>
        /// <exception cref="IOException">Throw when file or attribute is no available.</exception>
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

            byte[] buffer = Encoding.UTF8.GetBytes(attribValue);
            long result = SetXAttr(path, attribName, buffer, buffer.Length, 0, 0);

            if (result == -1)
            {
                ThrowLastException(path, attribName);
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

            long result = RemoveXAttr(path, attribName, 0);

            if (result == -1)
            {
                ThrowLastException(path, attribName);
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
        /// Throws corresponding exception for last platform api call.
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="attrName">Attribute name.</param>
        /// <exception cref="System.IO.IOException"></exception>
        private void ThrowLastException(string fileName, string attrName)
        {
            int errno = Marshal.GetLastWin32Error(); // It returns glibc errno
            string message = GetMessageForErrno(errno);

            throw new IOException(string.Format("[{0}:{1}] {2} Errno {3}", fileName, attrName, message, errno));
        }

        /// <summary>
        /// Returns error message that described error number.
        /// </summary>
        /// <param name="errno">Error number.</param>
        /// <returns>Error message</returns>
        private static string GetMessageForErrno(int errno)
        {
            StringBuilder buffer = new StringBuilder(ErrorMessageBufferMaxSize);

            StrErrorR(errno, buffer, ErrorMessageBufferMaxSize);

            return buffer.ToString();
        }

        /// <summary>
        /// External func getxattr from libc, what returns custom attribute by name.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="buffer">Buffer to collect attribute value.</param>
        /// <param name="bufferSize">Buffer size.</param>
        /// <param name="position">Position value.</param>
        /// <param name="options">Options value.</param>
        /// <returns>ttribute value size in bytes, when returning value -1 than some error occurred./// </returns>
        [DllImport(libCName, EntryPoint = "getxattr", SetLastError = true)]
        extern static private long GetXAttr(string filePath, string attribName, byte[] attribValue, long size, long position, int options);

        /// <summary>
        /// External func setxattr from libc, sets attribute value for file by name. 
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value</param>
        /// <param name="size">Attribute value size</param>
        /// <param name="position">Position value.</param>
        /// <param name="options">Options value.</param>
        /// <returns>Status, when returning value -1 than some error occurred.</returns>
        [DllImport(libCName, EntryPoint = "setxattr", SetLastError = true)]
        extern static private long SetXAttr(string filePath, string attribName, byte[] attribValue, long size, long position, int options);

        /// <summary>
        /// Removes the extended attribute. 
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="options">Options value.</param>
        /// <returns>On success, zero is returned. On failure, -1 is returned.</returns>
        [DllImport(libCName, EntryPoint = "removexattr", SetLastError = true)]
        extern static private long RemoveXAttr(string path, string attribName, int options);

        /// <summary>
        /// External func listxattr from libc, what returns list of attributes separated null-terminated string.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="nameBuffer">Attribute name.</param>
        /// <param name="size">Buffer size</param>
        /// <param name="options">Options value.</param>
        /// <returns>Attributes bytes array size, when returning value -1 than some error occurred</returns>
        [DllImport(libCName, EntryPoint = "listxattr", SetLastError = true)]
        extern static private long ListXAattr(string filePath, StringBuilder nameBuffer, long size, int options);

        /// <summary>
        /// External func strerror_r from libc, what returns string that describes the error code passed in the argument.
        /// </summary>
        /// <param name="code">Error number.</param>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="bufferSize">Buffer size.</param>
        [DllImport(libCName, EntryPoint = "strerror_r", SetLastError = true)]
        extern static private IntPtr StrErrorR(int code, StringBuilder buffer, int bufferSize);
    }
}
