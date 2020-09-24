using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

using ITHit.Server;
using ITHit.WebDAV.Server;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItemAsync}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItemAsync>
    {

        /// <summary>
        /// Gets WebDAV Logger instance.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Singleton instance of <see cref="WebSocketsService"/>.
        /// </summary>
        public WebSocketsService socketService { get; private set; }
        /// <summary>
        /// Singleton instance of <see cref="DataLakeStoreService"/>
        /// </summary>
        public IDataCloudStoreService DataLakeStoreService { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="logger">WebDAV Logger instance.</param>
        /// <param name="socketService">Singleton instance of <see cref="WebSocketsService"/>.</param>
        /// <param name="dataCloudStoreService">Singleton instance of <see cref="IDataCloudStoreService"/></param>
        public DavContext(IHttpContextAccessor httpContextAccessor, ILogger logger
            , WebSocketsService socketService, IDataCloudStoreService dataCloudStoreService
            )
            : base(httpContextAccessor.HttpContext)
        {
            Logger = logger;
            this.socketService = socketService;
            DataLakeStoreService = dataCloudStoreService;
        }

        /// <summary>
        /// Creates <see cref="IHierarchyItemAsync"/> instance by path.
        /// </summary>
        /// <param name="path">Item relative path including query string.</param>
        /// <returns>Instance of corresponding <see cref="IHierarchyItemAsync"/> or null if item is not found.</returns>
        public override async Task<IHierarchyItemAsync> GetHierarchyItemAsync(string path)
        {
            path = path.Trim(new[] { ' ', '/' });

            //remove query string.
            int ind = path.IndexOf('?');
            if (ind > -1)
            {
                path = path.Remove(ind);
            }
            if (!await DataLakeStoreService.ExistsAsync(path))
            {
                Logger.LogDebug("Could not find item that corresponds to path: " + path);
                return null; // no hierarchy item that corresponds to path parameter was found in the repository
            }

            IHierarchyItemAsync item;
            if (await DataLakeStoreService.IsDirectoryAsync(path))
            {
                item = await DavFolder.GetFolderAsync(this, path);
            }
            else
            {
                item = await DavFile.GetFileAsync(this, path);
            }
            return item;
        }

    }
}
