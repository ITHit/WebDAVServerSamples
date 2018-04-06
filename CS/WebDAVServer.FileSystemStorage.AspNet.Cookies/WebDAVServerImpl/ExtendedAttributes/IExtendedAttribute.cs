using System.Threading.Tasks;

namespace WebDAVServer.FileSystemStorage.AspNet.Cookies.ExtendedAttributes
{
    /// <summary>
    /// Provides methods for reading and writing extended attributes on files and folders.
    /// </summary>
    public interface IExtendedAttribute
    {
        /// <summary>
        /// Determines whether extended attributes are supported.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <returns>True if extended attributes or NTFS file alternative streams are supported, false otherwise.</returns>
        Task<bool> IsExtendedAttributesSupportedAsync(string path);

        /// <summary>
        /// Gets extended attribute or null if attribute or file not found.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value or null if attribute or file not found.</returns>
        Task<string> GetExtendedAttributeAsync(string path, string attribName);

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        Task SetExtendedAttributeAsync(string path, string attribName, string attribValue);

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        Task DeleteExtendedAttributeAsync(string path, string attribName);
    }
}
