using System.Threading.Tasks;

namespace CardDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes
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
        /// Checks extended attribute existence.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>True if attribute exist, false otherwise.</returns>
        Task<bool> HasExtendedAttributeAsync(string path, string attribName);

        /// <summary>
        /// Gets extended attribute or null if attribute or file not found.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
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

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        Task DeleteExtendedAttributes(string path);

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        Task CopyExtendedAttributes(string sourcePath, string destinationPath);

        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        Task MoveExtendedAttributes(string sourcePath, string destinationPath);
    }
}
