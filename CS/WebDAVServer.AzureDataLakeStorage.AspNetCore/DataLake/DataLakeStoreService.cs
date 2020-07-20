using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Storage;
using Azure.Storage.Files.DataLake;
using ITHit.WebDAV.Server;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Config;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake
{
    /// <summary>
    /// Provides method for interacting with Azure Data Lake.
    /// </summary>
    public class DataLakeStoreService : IDataLakeStoreService
    {
        private readonly DataLakeFileSystemClient dataLakeClient;
        /// <summary>
        /// Name of the custom attribute for LastModified property.
        /// </summary>
        private const string LastModifiedProperty = "LastModified";

        /// <summary>
        /// Initializes new instance of DataLakeExtendedAttribute.
        /// </summary>
        /// <param name="configuration">Context configuration.</param>
        public DataLakeStoreService(IOptions<DavContextConfig> configuration)
        {
            DavContextConfig config = configuration.Value;
            var sharedKeyCredential = new StorageSharedKeyCredential(config.AzureStorageAccountName, config.AzureStorageAccessKey);
            var dfsUri = "https://" + config.AzureStorageAccountName + ".dfs.core.windows.net";
            var dataLakeServiceClient = new DataLakeServiceClient(new Uri(dfsUri), sharedKeyCredential);
            dataLakeClient = dataLakeServiceClient.GetFileSystemClient(config.DataLakeContainerName);
        }
        /// <summary>
        /// Check item for existence.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <returns>Existence result.</returns>
        public async Task<ExistenceResult> ExistsAsync(string path)
        {
            var client = dataLakeClient.GetFileClient(path == "" ? "%2F" : path);
            var exists = await client.ExistsAsync();
            var isDirectory = false;
            if (exists.Value)
            {
                var props = await client.GetPropertiesAsync();
                isDirectory = props.Value.IsDirectory;
            }
            return new ExistenceResult {Exists = exists, IsDirectory = isDirectory};
        }
        /// <summary>
        /// Returns item info for the path. Doesn't check if item exists.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <returns><see cref="DataLakeItem"/></returns>
        public async Task<DataLakeItem> GetItemAsync(string path)
        {
            var client = await GetFileClient(path);
            var properties = await client.GetPropertiesAsync();
            var dlItem = new DataLakeItem
            {
                ContentLength = properties.Value.ContentLength,
                ContentType = properties.Value.ContentType,
                Name = EncodeUtil.DecodeUrlPart(client.Name),
                Path = client.Path,
                CreatedUtc = properties.Value.CreatedOn.UtcDateTime,
                ModifiedUtc = properties.Value.LastModified.UtcDateTime,
                Properties = properties.Value.Metadata,
                IsDirectory = properties.Value.IsDirectory
            };
            return dlItem;
        }
        /// <summary>
        /// Read item data to output stream
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <param name="output">Stream to read to.</param>
        /// <param name="startIndex">Index to start to read.</param>
        /// <param name="count">Size of data to read.</param>
        /// <returns></returns>
        public async Task ReadItemAsync(string path, Stream output, long startIndex, long count)
        {
            var client = await GetFileClient(path);
            var readData = await client.ReadAsync(new HttpRange(startIndex, count));
            await readData.Value.Content.CopyToAsync(output);
        }
        /// <summary>
        /// Writes item to Data Lake storage.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <param name="content">Stream to write from.</param>
        /// <param name="totalFileSize">Size of data to write.</param>
        /// <param name="customProps">Custom attributes.</param>
        /// <returns></returns>
        public async Task WriteItemAsync(string path, Stream content, long totalFileSize, IDictionary<string, string> customProps)
        {
            var client = await GetFileClient(path);
            await client.UploadAsync(content, true);
            await client.FlushAsync(totalFileSize);
            await client.SetMetadataAsync(customProps);
        }
        /// <summary>
        /// Copy item to another destination.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <param name="destFolder">Path to destination folder.</param>
        /// <param name="destName">Destination name.</param>
        /// <param name="contentLength">Size of item to copy.</param>
        /// <param name="sourceProps">Custom attributes to copy.</param>
        /// <returns></returns>
        public async Task CopyItemAsync(string path, string destFolder, string destName, long contentLength, IDictionary<string, string> sourceProps)
        {
            var sourceClient = await GetFileClient(path);
            var targetFolder = await GetDirectoryClient(destFolder);
            await CreateFileAsync(destFolder, destName);
            var memoryStream = new MemoryStream();
            await sourceClient.ReadToAsync(memoryStream);
            memoryStream.Position = 0;
            string targetPath = targetFolder.Path + "/" + EncodeUtil.EncodeUrlPart(destName);
            await WriteItemAsync(targetPath, memoryStream, contentLength, sourceProps);
            await CopyExtendedAttributes(new DataLakeItem{Properties = sourceProps}, targetPath);
            var destItem = new DataLakeItem
            {
                Name = destName,
                Path = targetPath
            };
            await DeleteExtendedAttributeAsync(destItem, "Locks");
        }

        /// <summary>
        /// Returns list of child items in current folder.
        /// </summary>
        /// <param name="relativePath">Relative path.</param>
        /// <returns>Returns list of child items in current folder.</returns>
        public async Task<IList<DataLakeItem>> GetChildrenAsync(string relativePath)
        {
            IList<DataLakeItem> children = new List<DataLakeItem>();
            await foreach (var pathItem in dataLakeClient.GetPathsAsync((EncodeUtil.DecodeUrlPart(relativePath))))
            {
                var path = pathItem.Name;
                var realName = path;
                if (path.Contains("/"))
                {
                    realName = path.Substring(path.LastIndexOf("/", StringComparison.Ordinal) + 1);
                }
                children.Add(new DataLakeItem
                {
                    ContentLength = pathItem.ContentLength ?? 0,
                    Name = realName,
                    Path = EncodePath(pathItem.Name),
                    CreatedUtc = pathItem.LastModified.UtcDateTime,
                    ModifiedUtc = pathItem.LastModified.UtcDateTime,
                    IsDirectory = pathItem.IsDirectory ?? false
                });
            }
            return children;
        }
        /// <summary>
        /// Creates new file.
        /// </summary>
        /// <param name="path">Path of folder to create new file.</param>
        /// <param name="name">Name of new file.</param>
        /// <returns></returns>
        public async Task CreateFileAsync(string path, string name)
        {
            if (path == "/")
            {
                await dataLakeClient.CreateFileAsync(name);
            }
            else
            {
                var dataLakeDirectoryClient = await GetDirectoryClient(path);
                await dataLakeDirectoryClient.CreateFileAsync(name);
            }
        }
        /// <summary>
        /// Creates new directory.
        /// </summary>
        /// <param name="path">Path of folder to create new directory.</param>
        /// <param name="name">Name of new folder.</param>
        /// <returns></returns>
        public async Task CreateDirectoryAsync(string path, string name)
        {
            if (path == "/")
            {
                await dataLakeClient.CreateDirectoryAsync(name);
            }
            else
            {
                var dataLakeDirectoryClient = await GetDirectoryClient(path);
                await dataLakeDirectoryClient.CreateSubDirectoryAsync(name);
            }
        }
        /// <summary>
        /// Deletes item by path.
        /// </summary>
        /// <param name="path">Path to item.</param>
        /// <returns></returns>
        public async Task DeleteItemAsync(string path)
        {
            if (path.StartsWith("/"))
            {
                path = path.Substring(1);
            }
            DataLakePathClient client = await GetFileClient(path);
            await client.DeleteAsync(true);
        }

        /// <summary>
        /// Gets extended attribute.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="attribName">Attribute name.</param>
        /// <returns>Attribute value.</returns>
        public async Task<T> GetExtendedAttributeAsync<T>(DataLakeItem dataLakeItem, string attribName) where T : new()
        {
            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            dataLakeItem.Properties.TryGetValue(attribName, out string value);
            return await Task.Run(() => Deserialize<T>(value));
        }

        /// <summary>
        /// Sets extended attribute.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="attribName">Attribute name.</param>
        /// <param name="attribValue">Attribute value.</param>
        public async Task SetExtendedAttributeAsync(DataLakeItem dataLakeItem, string attribName, object attribValue)
        {
            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            if (attribValue == null)
            {
                throw new ArgumentNullException("attribValue");
            }
            if (!dataLakeItem.Properties.ContainsKey(LastModifiedProperty))
            {
                DateTime lastWriteTimeUtc = dataLakeItem.ModifiedUtc;
                dataLakeItem.Properties[LastModifiedProperty] = (lastWriteTimeUtc.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds.ToString(CultureInfo.InvariantCulture);
            }
            var fileClient = dataLakeClient.GetFileClient(dataLakeItem.Path);
            string serializedValue = Serialize(attribValue);
            dataLakeItem.Properties[attribName] = serializedValue;
            await fileClient.SetMetadataAsync(dataLakeItem.Properties);
        }

        /// <summary>
        /// Deletes extended attribute.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="attribName">Attribute name.</param>
        public async Task DeleteExtendedAttributeAsync(DataLakeItem dataLakeItem, string attribName)
        {
            if (string.IsNullOrEmpty(attribName))
            {
                throw new ArgumentNullException("attribName");
            }

            var fileClient = dataLakeClient.GetFileClient(dataLakeItem.Path);
            dataLakeItem.Properties.Remove(attribName);
            await fileClient.SetMetadataAsync(dataLakeItem.Properties);
        }

        /// <summary>
        /// Deletes all extended attributes.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        public async Task DeleteExtendedAttributes(DataLakeItem dataLakeItem)
        {
            var fileClient = dataLakeClient.GetFileClient(dataLakeItem.Path);
            dataLakeItem.Properties.Clear();
            await fileClient.SetMetadataAsync(dataLakeItem.Properties);
        }

        /// <summary>
        /// Copies all extended attributes.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="destPath">Destination path.</param>
        public async Task CopyExtendedAttributes(DataLakeItem dataLakeItem, string destPath)
        {
            var destClient = await GetFileClient(destPath);
            await destClient.SetMetadataAsync(dataLakeItem.Properties);
        }

        /// <summary>
        /// Moves all extended attributes.
        /// </summary>
        /// <param name="dataLakeItem"><see cref="DataLakeItem"/></param>
        /// <param name="destPath">Destination path.</param>
        public async Task MoveExtendedAttributes(DataLakeItem dataLakeItem, string destPath)
        {
            await CopyExtendedAttributes(dataLakeItem, destPath);
            await DeleteExtendedAttributes(dataLakeItem);
        }

        /// <summary>
        /// Serializes object to XML string.
        /// </summary>
        /// <param name="data">Object to be serialized.</param>
        /// <returns>String representation of an object.</returns>
        private static string Serialize(object data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            return JsonConvert.SerializeObject(data);
        }

        /// <summary>
        /// Deserializes XML string to an object of a specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlString">XML string to be deserialized.</param>
        /// <returns>Deserialized object. If xmlString is empty or null returns new empty instance of an object.</returns>
        private static T Deserialize<T>(string xmlString) where T : new()
        {
            if (string.IsNullOrEmpty(xmlString))
            {
                return new T();
            }

            return JsonConvert.DeserializeObject<T>(xmlString);
        }

        /// <summary>
        /// Encodes parts of the path.
        /// </summary>
        /// <param name="relativePath">Relative path to encode.</param>
        private static string EncodePath(string relativePath)
        {
            string[] decodedParts = relativePath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string[] encodedParts = decodedParts.Select(EncodeUtil.EncodeUrlPart).ToArray();
            return "/" + string.Join("/", encodedParts);
        }

        /// <summary>
        /// Helper method to create directory client by path.
        /// </summary>
        /// <param name="relativePath">Relative path of item.</param>
        /// <returns>DataLakeFileSystemClient or null if item is not exists.</returns>
        private async Task<DataLakeDirectoryClient> GetDirectoryClient(string relativePath)
        {
            return await Task.Run(() => dataLakeClient.GetDirectoryClient(relativePath == "" ? "%2F" : relativePath));
        }

        /// <summary>
        /// Helper method to create file client by path.
        /// </summary>
        /// <param name="relativePath">Relative path of item.</param>
        /// <returns>DataLakeFileClient or null if item is not exists.</returns>
        private async Task<DataLakeFileClient> GetFileClient(string relativePath)
        {
            return await Task.Run(() => dataLakeClient.GetFileClient(relativePath == "" ? "%2F" : relativePath));
        }
    }
}
