using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Azure.Storage.Files.DataLake;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.ExtendedAttributes
{
    /// <summary>
    /// Provides methods for reading and writing extended attributes on Data Lake custom properties.
    /// </summary>
    public class DataLakeExtendedAttribute : IExtendedAttribute
    {
        private readonly DataLakeFileSystemClient dataLakeClient;
        private DLItem dlItem;

        /// <summary>
        /// Initializes new instance of DataLakeExtendedAttribute.
        /// </summary>
        /// <param name="dataLakeClient">Data Lake client.</param>
        public DataLakeExtendedAttribute(DataLakeFileSystemClient dataLakeClient)
        {
            this.dataLakeClient = dataLakeClient;
        }
        /// <summary>
        /// Set <see cref="DLItem"> to use for operation.</see>
        /// </summary>
        /// <param name="dlItem"> DLItem instance.</param>
        /// <returns></returns>
        internal async Task UseDlItem(DLItem dlItem)
        {
            this.dlItem = dlItem;
        }

        /// <summary>
        /// Determines whether extended attributes are supported. 
        /// </summary>
        /// <param name="checkPath">File or folder path.</param>
        /// <returns>True if extended attributes are supported, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Throw when path is null or empty.</exception>
        /// <exception cref="COMException">Throw when happens some system exception.</exception>
        public async Task<bool> IsExtendedAttributesSupportedAsync(string checkPath)
        {
            return true;
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

            return dlItem.Properties.ContainsKey(attribName);
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
                throw new ArgumentNullException(nameof(path));
            }

            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            dlItem.Properties.TryGetValue(attribName, out string value);
            return value;

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

            var fileClient = dataLakeClient.GetFileClient(path);
            if (!dlItem.Properties.ContainsKey(attribName))
            {
                dlItem.Properties.Add(attribName, attribValue);
            }
            else
            {
                dlItem.Properties[attribName] = attribValue;
            }
            await fileClient.SetMetadataAsync(dlItem.Properties);
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

            var fileClient = dataLakeClient.GetFileClient(path);
            dlItem.Properties.Remove(attribName);
            await fileClient.SetMetadataAsync(dlItem.Properties);
        }

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        public async Task DeleteExtendedAttributes(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }
            var fileClient = dataLakeClient.GetFileClient(path);
            dlItem.Properties.Clear();
            await fileClient.SetMetadataAsync(dlItem.Properties);
        }

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        public async Task CopyExtendedAttributes(string sourcePath, string destinationPath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                throw new ArgumentNullException("sourcePath");
            }
            if (string.IsNullOrEmpty(destinationPath))
            {
                throw new ArgumentNullException("destinationPath");
            }

            var destClient = dataLakeClient.GetFileClient(destinationPath);
            await destClient.SetMetadataAsync(dlItem.Properties);
        }

        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        public async Task MoveExtendedAttributes(string sourcePath, string destinationPath)
        {
            await CopyExtendedAttributes(sourcePath, destinationPath);
            await DeleteExtendedAttributes(sourcePath);
        }
    }
}
