using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CalDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes
{
    /// <summary>
    /// Provides methods for reading and writing extended attributes on files and folders.
    /// NTFS alternate data streams are used to store attributes.
    /// </summary>
    public class ExtendedAttribute : IExtendedAttribute
    {
        private readonly string pathFormat = "{0}:{1}";

        /// <summary>
        /// Checks extended attribute existence.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>True if attribute exist, false otherwise.</returns>
        public async Task<bool> HasExtendedAttributeAsync(string path, string attribName)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(attribName)) throw new ArgumentNullException(nameof(attribName));

            bool attributeExists = true;
            string fullPath = string.Format(pathFormat, path, attribName);

            if (!File.Exists(fullPath))
            {
                attributeExists = false;
            }
            return attributeExists;
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

            string fullPath = string.Format(pathFormat, path, attribName);
            File.Delete(fullPath);
        }

        /// <summary>
        /// Gets extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value or null if attribute or file not found.</returns>
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

            string fullPath = string.Format(pathFormat, path, attribName);
            if (File.Exists(fullPath))
            {
                await using (FileStream fileStream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                using (StreamReader streamReader = new StreamReader(fileStream))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }

            return null;
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

            string fullPath = string.Format(pathFormat, path, attribName);
            await using (FileStream fileStream = File.Open(fullPath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete))
            await using (StreamWriter streamWriter = new StreamWriter(fileStream))
            {
                await streamWriter.WriteAsync(attribValue);
            }
        }

        public Task DeleteExtendedAttributes(string path)
        {
            throw new NotImplementedException();
        }

        public Task MoveExtendedAttributes(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }

        public Task CopyExtendedAttributes(string sourcePath, string destinationPath)
        {
            throw new NotImplementedException();
        }
    }
}
