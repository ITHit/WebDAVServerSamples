using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using ITHit.Server;
using ITHit.WebDAV.Server;
using AzureDataLakeStorage.Configuration;
using AzureDataLakeStorage.ExtendedAttributes;
using Azure.Storage.Files.DataLake;
using Azure.Storage;
using Azure.Storage.Files.DataLake.Models;
using AzureDataLakeStorage.Config;

namespace AzureDataLakeStorage
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItemAsync}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItemAsync>
    {

        /// <summary>
        /// Context configuration
        /// </summary>
        public DavContextConfig Config { get; private set; }

        /// <summary>
        /// Gets WebDAV Logger instance.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Singleton instance of <see cref="WebSocketsService"/>.
        /// </summary>
        public WebSocketsService socketService { get; private set; }

        private DataLakeFileSystemClient fileSystemClient;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="config">WebDAV Context configuration.</param>
        /// <param name="logger">WebDAV Logger instance.</param>
        /// <param name="socketService">Singleton instance of <see cref="WebSocketsService"/>.</param>
        public DavContext(IHttpContextAccessor httpContextAccessor, IOptions<DavContextConfig> config, ILogger logger
            , WebSocketsService socketService
            )
            : base(httpContextAccessor.HttpContext)
        {
            Config = config.Value;
            Logger = logger;
            this.socketService = socketService;
        }

        /// <summary>
        /// Creates <see cref="IHierarchyItemAsync"/> instance by path.
        /// </summary>
        /// <param name="path">Item relative path including query string.</param>
        /// <returns>Instance of corresponding <see cref="IHierarchyItemAsync"/> or null if item is not found.</returns>
        public override async Task<IHierarchyItemAsync> GetHierarchyItemAsync(string path)
        {
            Trace.TraceWarning("GetHierarchyItemAsync" + path);
            path = path.Trim(new[] { ' ', '/' });

            //remove query string.
            int ind = path.IndexOf('?');
            if (ind > -1)
            {
                path = path.Remove(ind);
            }

            IHierarchyItemAsync item = null;

            item = await DavFolder.GetFolderAsync(this, path);
            if (item != null)
                return item;

            item = await DavFile.GetFileAsync(this, path);
            if (item != null)
                return item;

            Logger.LogDebug("Could not find item that corresponds to path: " + path);

            return null; // no hierarchy item that corresponds to path parameter was found in the repository
        }

        /// <summary>
        /// Initializes <see cref="DataLakeFileSystemClient"> instance once.</see>
        /// </summary>
        /// <returns></returns>
        internal DataLakeFileSystemClient GetFileSystemClient()
        {
            if (fileSystemClient == null)
            {
                StorageSharedKeyCredential sharedKeyCredential = new StorageSharedKeyCredential(Config.AzureStorageAccountName, Config.AzureStorageAccessKey);
                string dfsUri = "https://" + Config.AzureStorageAccountName + ".dfs.core.windows.net";
                var dataLakeServiceClient = new DataLakeServiceClient(new Uri(dfsUri), sharedKeyCredential);
                fileSystemClient = dataLakeServiceClient.GetFileSystemClient(Config.DataLakeContainerName);
                DataLakeAttributeExtension.UseDataLakeAttribute(fileSystemClient);
            }
            return fileSystemClient;
        }
        /// <summary>
        /// Helper method to create directory client by path.
        /// </summary>
        /// <param name="relativePath">Relative path of item.</param>
        /// <param name="skipExistenceCheck">True if it needs to skip check of item existence.</param>
        /// <returns>DataLakeFileSystemClient or null if item is not exists.</returns>
        internal async Task<DataLakeDirectoryClient> GetDirectoryClient(string relativePath, bool skipExistenceCheck = false)
        {
            var dataLakeFileSystemClient = GetFileSystemClient();

            var dataLakeDirectoryClient = dataLakeFileSystemClient.GetDirectoryClient(relativePath == "" ? "%2F" : relativePath);
            if (skipExistenceCheck)
            {
                return dataLakeDirectoryClient;
            }
            var test = dataLakeDirectoryClient.ExistsAsync();
            if (await test)
            {
                var isDirectory = dataLakeDirectoryClient.GetPropertiesAsync().Result.Value.IsDirectory;
                if (isDirectory)
                {
                    return dataLakeDirectoryClient;
                }
            }
            return null;
        }
        /// <summary>
        /// Helper method to create file client by path.
        /// </summary>
        /// <param name="relativePath">Relative path of item.</param>
        /// <param name="skipExistenceCheck">True if it needs to skip check of item existence.</param>
        /// <returns>DataLakeFileClient or null if item is not exists.</returns>
        internal async Task<DataLakeFileClient> GetFileClient(string relativePath, bool skipExistenceCheck = false)
        {
            var dataLakeFileClient = GetFileSystemClient().GetFileClient(relativePath);
            if (skipExistenceCheck)
            {
                return dataLakeFileClient;
            }
            return await dataLakeFileClient.ExistsAsync() ? dataLakeFileClient : null;
        }
    }
}
