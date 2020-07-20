using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake
{
    /// <summary>
    /// Provides method for interacting with Azure Data Lake.
    /// </summary>
    public interface IDataLakeStoreService
    {
        /// <summary>
        /// Check item for existence.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <returns>Existence result.</returns>
        Task<ExistenceResult> ExistsAsync(string path);
        /// <summary>
        /// Returns item info for the path. Doesn't check if item exists.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <returns><see cref="DataLakeItem"/></returns>
        Task<DataLakeItem> GetItemAsync(string path);
        /// <summary>
        /// Read item data to output stream
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <param name="output">Stream to read to.</param>
        /// <param name="startIndex">Index to start to read.</param>
        /// <param name="count">Size of data to read.</param>
        /// <returns></returns>
        Task ReadItemAsync(string path, Stream output, long startIndex, long count);
        /// <summary>
        /// Writes item to Data Lake storage.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <param name="content">Stream to write from.</param>
        /// <param name="totalFileSize">Size of data to write.</param>
        /// <param name="customProps">Custom attributes.</param>
        /// <returns></returns>
        Task WriteItemAsync(string path, Stream content, long totalFileSize, IDictionary<string, string> customProps);
        /// <summary>
        /// Copy item to another destination.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <param name="destFolder">Path to destination folder.</param>
        /// <param name="destName">Destination name.</param>
        /// <param name="contentLength">Size of item to copy.</param>
        /// <param name="sourceProps">Custom attributes to copy.</param>
        /// <returns></returns>
        Task CopyItemAsync(string path, string destFolder, string destName, long contentLength, IDictionary<string, string> sourceProps);
        /// <summary>
        /// Returns list of child items in current folder.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Returns list of child items in current folder.</returns>
        Task<IList<DataLakeItem>> GetChildrenAsync(string relativePath);
        /// <summary>
        /// Creates new file.
        /// </summary>
        /// <param name="path">Path of folder to create new file.</param>
        /// <param name="name">Name of new file.</param>
        /// <returns></returns>
        Task CreateFileAsync(string path, string name);
        /// <summary>
        /// Creates new directory.
        /// </summary>
        /// <param name="path">Path of folder to create new directory.</param>
        /// <param name="name">Name of new folder.</param>
        /// <returns></returns>
        Task CreateDirectoryAsync(string path, string name);
        /// <summary>
        /// Deletes item by path.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <returns></returns>
        Task DeleteItemAsync(string path);

        /// <summary>
        /// Gets extended attribute.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        Task<T> GetExtendedAttributeAsync<T>(DataLakeItem dataLakeItem, string attribName) where T : new();

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        Task SetExtendedAttributeAsync(DataLakeItem dataLakeItem, string attribName, object attribValue);

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="attribName">Attribute name.</param>
        Task DeleteExtendedAttributeAsync(DataLakeItem dataLakeItem, string attribName);

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        Task DeleteExtendedAttributes(DataLakeItem dataLakeItem);

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="destPath">Destination path.</param>
        Task CopyExtendedAttributes(DataLakeItem dataLakeItem, string destPath);

        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="destPath">Destination path.</param>
        Task MoveExtendedAttributes(DataLakeItem dataLakeItem, string destPath);
    }

    /// <summary>
    /// Represents Data Lake item.
    /// </summary>
    public class DataLakeItem
    {
        /// <summary>
        /// Name of the item.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// Relative path of the item in the Data Lake.
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// Content Type of the item.
        /// </summary>
        public string ContentType { get; set; } = string.Empty;
        /// <summary>
        /// Content length of the item.
        /// </summary>
        public long ContentLength { get; set; }
        /// <summary>
        /// Created time of the item in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Modified time of the item in UTC.
        /// </summary>
        public DateTime ModifiedUtc { get; set; } = DateTime.MinValue;
        public bool IsDirectory { get; set; }
        /// <summary>
        /// Custom properties of the item.
        /// </summary>
        public IDictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Class which represents existence response
    /// </summary>
    public class ExistenceResult
    {
        /// <summary>
        /// Flag shows if item exists.
        /// </summary>
        public bool Exists { get; set; }
        /// <summary>
        /// Flag shows if item is folder.
        /// </summary>
        public bool IsDirectory { get; set; }
    }
}