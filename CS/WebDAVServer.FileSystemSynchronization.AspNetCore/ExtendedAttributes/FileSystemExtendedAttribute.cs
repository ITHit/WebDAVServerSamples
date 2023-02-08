using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace WebDAVServer.FileSystemSynchronization.AspNetCore.ExtendedAttributes
{
    /// <summary>
    /// Provides extension methods to read and write extended attributes on file and folders.
    /// </summary>
    /// <remarks>This class uses file system to store extended attributes in case of alternative data streams not supported.</remarks>
    public class FileSystemExtendedAttribute : IExtendedAttribute
    {
        /// <summary>
        /// Gets path used to store extended attributes data.
        /// </summary>
        public string StoragePath { get; }

        /// <summary>
        /// Gets path directory used as root of files. If set stores attributes in storage relative to it.
        /// </summary>
        public string DataStoragePath { get; }

        /// <summary>
        /// Creates instance of <see cref="FileSystemExtendedAttribute"/>
        /// </summary>
        /// <param name="attrStoragePath">Used as path to store attributes data.</param>
        /// <param name="dataStoragePath">Used as path to store attributes data path relative of.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="attrStoragePath"/> or <paramref name="dataStoragePath"/> is <c>null</c> or an empty string.</exception>
        public FileSystemExtendedAttribute(string attrStoragePath, string dataStoragePath)
        {
            if (string.IsNullOrEmpty(attrStoragePath)) throw new ArgumentNullException(nameof(attrStoragePath));
            if (string.IsNullOrEmpty(dataStoragePath)) throw new ArgumentNullException(nameof(dataStoragePath));

            this.StoragePath = System.IO.Path.GetFullPath(attrStoragePath);
            this.DataStoragePath = System.IO.Path.GetFullPath(dataStoragePath);
        }

        /// <summary>
        /// Determines whether extended attributes are supported.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <returns>True if extended attributes or NTFS file alternative streams are supported, false otherwise.</returns>
        public async Task<bool> IsExtendedAttributesSupportedAsync(string path)
        {
            return false;
        }

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
            string attrPath = this.GetAttrFullPath(path, attribName);

            if (!File.Exists(attrPath))
            {
                attributeExists = false;
            }
            return attributeExists;
        }

        /// <summary>
        /// Gets extended attribute or null if attribute or file not found.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        public async Task<string> GetExtendedAttributeAsync(string path, string attribName)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (string.IsNullOrEmpty(attribName)) throw new ArgumentNullException(nameof(attribName));


            string attrPath = this.GetAttrFullPath(path, attribName);

            await using (FileStream fileStream = File.Open(attrPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fileStream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Gets the file path where attribute files stores.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName"> The attribute name.</param>
        /// <returns>The full file path relative to <see cref="StoragePath"/> depends on <see cref="DataStoragePath"/>.</returns>
        private string GetAttrFullPath(string path, string attribName)
        {
            string attrRootPath = GetAttrRootPath(path);
            return System.IO.Path.Combine(attrRootPath, attribName);
        }

        private static string GetPathWithoutVolumeSeparator(string path)
        {
            if(System.IO.Path.VolumeSeparatorChar == System.IO.Path.DirectorySeparatorChar) return path;
            return path.Replace(System.IO.Path.VolumeSeparatorChar.ToString(), string.Empty);
        }

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        public async Task SetExtendedAttributeAsync(string path, string attribName, string attribValue)
        {
            if(string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if(string.IsNullOrEmpty(attribName)) throw new ArgumentNullException(nameof(attribName));
            if(string.IsNullOrEmpty(attribValue)) throw new ArgumentNullException(nameof(attribValue));

            string attrSubTreePath = this.GetAttrRootPath(path);
            if(!Directory.Exists(attrSubTreePath))
            {
                Directory.CreateDirectory(attrSubTreePath);
            }

            string attrPath = System.IO.Path.Combine(attrSubTreePath, attribName);
            File.WriteAllText(attrPath, attribValue, Encoding.UTF8);
        }

        /// <summary>
        /// Gets the directory where attribute files stores.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <returns>The full directory path relative to <see cref="StoragePath"/> depends on <see cref="DataStoragePath"/>.</returns>
        private string GetAttrRootPath(string path)
        {
            if(!string.IsNullOrEmpty(DataStoragePath))
            {
                return System.IO.Path.Combine(this.StoragePath, GetSubDirectoryPath(DataStoragePath, path));
            }

            string encodedPath = GetPathWithoutVolumeSeparator(System.IO.Path.GetFullPath(path));
            encodedPath = encodedPath.TrimStart(System.IO.Path.DirectorySeparatorChar);
            return System.IO.Path.Combine(this.StoragePath, encodedPath);
        }

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        /// <param name="attribName">Attribute name.</param>
        public async Task DeleteExtendedAttributeAsync(string path, string attribName)
        {
            if(string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if(string.IsNullOrEmpty(attribName)) throw new ArgumentNullException(nameof(attribName));

            string attrPath = this.GetAttrFullPath(path, attribName);
            if(File.Exists(attrPath)) File.Delete(attrPath);
        }

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="path">File or folder path.</param>
        public async Task DeleteExtendedAttributes(string path)
        {
            if(string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string attrSubTreePath = this.GetAttrRootPath(path);
            if(Directory.Exists(attrSubTreePath))
            {
                Directory.Delete(attrSubTreePath, true);
            }
        }

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        public async Task CopyExtendedAttributes(string sourcePath, string destinationPath)
        {
            if(string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if(string.IsNullOrEmpty(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

            string sourceSubTreePath = this.GetAttrRootPath(sourcePath);
            if(!Directory.Exists(sourceSubTreePath))
            {
                return;
            }

            string destSubTreePath = this.GetAttrRootPath(destinationPath);
            DirectoryCopy(sourceSubTreePath, destSubTreePath);
        }

        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="sourcePath">The source path. </param>
        /// <param name="destinationPath">The target pat.</param>
        public async Task MoveExtendedAttributes(string sourcePath, string destinationPath)
        {
            if(string.IsNullOrEmpty(sourcePath)) throw new ArgumentNullException(nameof(sourcePath));
            if(string.IsNullOrEmpty(destinationPath)) throw new ArgumentNullException(nameof(destinationPath));

            string sourceSubTreePath = this.GetAttrRootPath(sourcePath);
            if(!Directory.Exists(sourceSubTreePath))
            {
                return;
            }

            string destSubTreePath = this.GetAttrRootPath(destinationPath);
            DirectoryInfo parentDirectory = Directory.GetParent(destSubTreePath);
            if(!parentDirectory.Exists)
            {
                parentDirectory.Create();
            }

            Directory.Move(sourceSubTreePath, destSubTreePath);
        }

        /// <summary>
        /// Copies directory and its contents to a new location.
        /// </summary>
        /// <param name="sourceDirName">The path of the file or directory to copy.</param>
        /// <param name="destDirName">The path to the new location for <paramref name="sourceDirName"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="sourceDirName"/> or <paramref name="destDirName"/> is <c>null</c> or an empty string.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="sourceDirName"/> does not exists.</exception>
        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            if (string.IsNullOrEmpty(sourceDirName)) throw new ArgumentNullException(nameof(sourceDirName));
            if (string.IsNullOrEmpty(destDirName)) throw new ArgumentNullException(nameof(destDirName));

            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if(!dir.Exists)
            {
                throw new DirectoryNotFoundException("Source directory does not exist or could not be found: " + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            if(!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            FileInfo[] files = dir.GetFiles();
            foreach(FileInfo file in files)
            {
                string destPath = System.IO.Path.Combine(destDirName, file.Name);
                file.CopyTo(destPath, false);
            }

            foreach(DirectoryInfo subdir in dirs)
            {
                string destPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, destPath);
            }
        }

        /// <summary>
        /// Create a sub directory path from one path to another. Paths will be resolved before calculating the difference.
        /// </summary>
        /// <param name="relativeTo">The source path the output should sub directory of. This path is always considered to be a directory.</param>
        /// <param name="path">The destination path.</param>
        /// <returns>The sub directory path path or <paramref name="path"/> if the paths don't sub directory of <paramref name="path"/>.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="relativeTo"/> or <paramref name="path"/> is <c>null</c> or an empty string.</exception>
        private static string GetSubDirectoryPath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentNullException(nameof(relativeTo));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            string fullRelativeTo = System.IO.Path.GetFullPath(relativeTo);
            string fullPath = System.IO.Path.GetFullPath(path);

            if(!fullPath.StartsWith(fullRelativeTo, StringComparison.InvariantCulture))
            {
                return fullPath;
            }

            fullRelativeTo = AppendTrailingSeparator(fullRelativeTo);
            fullPath = AppendTrailingSeparator(fullPath);

            string relativePath = fullPath.Replace(fullRelativeTo, string.Empty);
            if (string.IsNullOrEmpty(relativePath))
            {
                return fullPath;
            }

            return relativePath.TrimEnd(System.IO.Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Adds <see cref="System.IO.Path.DirectorySeparatorChar"/> to end of string if not exists.
        /// </summary>
        /// <param name="fullPath">The string to add.</param>
        /// <returns>The string with <see cref="System.IO.Path.DirectorySeparatorChar"/> at the end</returns>
        private static string AppendTrailingSeparator(string fullPath)
        {
            if(!fullPath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()))
            {
                fullPath += System.IO.Path.DirectorySeparatorChar;
            }

            return fullPath;
        }
    }
}