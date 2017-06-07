using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes
{
    /// <summary>
    /// Provides methods for reading and writing extended attributes on files and folders on Linux.
    /// </summary>
    public class LinuxExtendedAttribute : IExtendedAttribute
    {
        /// <summary>
        /// Errno for not existing attribute.
        /// </summary>
        private const int AttributeNotFoundErrno = 61;

        /// <summary>
        /// Dynamic C library name.
        /// </summary>
        private const string libCName = "libc.so.6";

        /// <summary>
        /// Linux allows stored extended attribute in special namespaces only.
        /// Extended user attributes.
        /// http://manpages.ubuntu.com/manpages/wily/man5/attr.5.html
        /// </summary>
        private readonly string attributeNameFormat = "user.{0}";

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

            long attributeCount = ListXAattr(path, null, 0);
            return attributeCount != -1;
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

            string userAttributeName = string.Format(attributeNameFormat, attribName);
            long attributeSize = GetXAttr(path, userAttributeName, null, 0);

            if (attributeSize == -1)
            {
                if (Marshal.GetLastWin32Error() == AttributeNotFoundErrno)
                {
                    return null;
                }

                ThrowLastException(path, userAttributeName);
            }

            byte[] buffer = new byte[attributeSize];
            long readedLength = GetXAttr(path, userAttributeName, buffer, attributeSize);

            if (readedLength == -1)
            {
                ThrowLastException(path, userAttributeName);
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

            string userAttributeName = string.Format(attributeNameFormat, attribName);

            byte[] buffer = Encoding.UTF8.GetBytes(attribValue);
            long result = SetXAttr(path, userAttributeName, buffer, buffer.Length, 0);

            if (result == -1)
            {
                ThrowLastException(path, userAttributeName);
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

            string userAttributeName = string.Format(attributeNameFormat, attribName);
            long result = RemoveXAttr(path, userAttributeName);

            if (result == -1)
            {
                ThrowLastException(path, userAttributeName);
            }
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
            // Init locale structure
            IntPtr locale = NewLocale(8127, "C", IntPtr.Zero); // LC_ALL_MASK - 8127

            if (locale == IntPtr.Zero)
            {
                throw new InvalidOperationException("Not able to get locale");
            }

            // Get error message for error number
            string message = Marshal.PtrToStringAnsi(StrErrorL(errno, locale));

            // Free locale
            FreeLocale(locale);

            return message;
        }
        
        /// <summary>
        /// External func getxattr from libc, what returns custom attribute by name.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="buffer">Buffer to collect attribute value.</param>
        /// <param name="bufferSize">Buffer size.</param>
        /// <returns>Attribute value size in bytes, when returning value -1 than some error occurred.</returns>
        [DllImport(libCName, EntryPoint = "getxattr", SetLastError = true)]
        extern static private long GetXAttr(string filePath, string attribName, byte[] buffer, long bufferSize);

        /// <summary>
        /// External func setxattr from libc, sets attribute value for file by name. 
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value</param>
        /// <param name="size">Attribute value size</param>
        /// <param name="flags">Flags.</param>
        /// <returns>Status, when returning value -1 than some error occurred.</returns>
        [DllImport(libCName, EntryPoint = "setxattr", SetLastError = true)]
        extern static private long SetXAttr(string filePath, string attribName, byte[] attribValue, long size, int flags);

        /// <summary>
        /// Removes the extended attribute. 
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>On success, zero is returned. On failure, -1 is returned.</returns>
        [DllImport(libCName, EntryPoint = "removexattr", SetLastError = true)]
        extern static private long RemoveXAttr(string path, string attribName);

        /// <summary>
        /// External func listxattr from libc, what returns list of attributes separated null-terminated string.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <param name="nameBuffer">Attribute name.</param>
        /// <param name="size">Buffer size</param>
        /// <returns>Attributes bytes array size, when returning value -1 than some error occurred</returns>
        [DllImport(libCName, EntryPoint = "listxattr", SetLastError = true)]
        extern static private long ListXAattr(string filePath, StringBuilder nameBuffer, long size);

        /// <summary>
        /// External func newlocale from libc, what initializes locale.
        /// </summary>
        /// <param name="mask">Category mask.</param>
        /// <param name="locale">Locale name.</param>
        /// <param name="oldLocale">Old locale.</param>
        /// <returns>Pointer to locale structure.</returns>
        [DllImport(libCName, EntryPoint = "newlocale", SetLastError = true)]
        extern static private IntPtr NewLocale(int mask, string locale, IntPtr oldLocale);

        /// <summary>
        /// External func freelocale from libc, what deallocates locale.
        /// </summary>
        /// <param name="locale">Locale structure.</param>
        [DllImport(libCName, EntryPoint = "freelocale", SetLastError = true)]
        extern static private void FreeLocale(IntPtr locale);

        /// <summary>
        /// External func strerror_l from libc, what returns string that describes the error code passed in the argument.
        /// </summary>
        /// <param name="code">Error code.</param>
        /// <param name="locale">Locale to display message in.</param>
        /// <returns>Localized error message</returns>
        [DllImport(libCName, EntryPoint = "strerror_l", SetLastError = true)]
        extern static private IntPtr StrErrorL(int code, IntPtr locale);
    }
}
