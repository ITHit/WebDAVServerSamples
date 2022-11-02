using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Azure;

using ITHit.Server;
using ITHit.WebDAV.Server;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Search;
using Microsoft.AspNetCore.Authentication;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItem"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItem>
    {
        public HttpContext HttpContext { get; private set; }

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
        /// Singleton instance of <see cref="CognitiveSearchService"/>
        /// </summary>
        public ICognitiveSearchService CognitiveSearchService { get; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="logger">WebDAV Logger instance.</param>
        /// <param name="socketService">Singleton instance of <see cref="WebSocketsService"/>.</param>
        /// <param name="dataCloudStoreService">Singleton instance of <see cref="IDataCloudStoreService"/></param>
        /// <param name="cognitiveSearchService">Singleton instance of <see cref="ICognitiveSearchService"/></param>
        public DavContext(IHttpContextAccessor httpContextAccessor, ILogger logger,
            WebSocketsService socketService,
            IDataCloudStoreService dataCloudStoreService,
            ICognitiveSearchService cognitiveSearchService
            )
            : base(httpContextAccessor.HttpContext)
        {
            Logger = logger;
            this.socketService = socketService;
            DataLakeStoreService = dataCloudStoreService;
            CognitiveSearchService = cognitiveSearchService;
            HttpContext = httpContextAccessor.HttpContext;
        }

        /// <summary>
        /// Creates <see cref="IHierarchyItem"/> instance by path.
        /// </summary>
        /// <param name="path">Item relative path including query string.</param>
        /// <returns>Instance of corresponding <see cref="IHierarchyItem"/> or null if item is not found.</returns>
        public override async Task<IHierarchyItem> GetHierarchyItemAsync(string path)
        {
            path = path.Trim(new[] { ' ', '/' });

            //remove query string.
            int ind = path.IndexOf('?');
            if (ind > -1)
            {
                path = path.Remove(ind);
            }

            try
            {
                if (!await DataLakeStoreService.ExistsAsync(path))
                {
                    Logger.LogDebug("Could not find item that corresponds to path: " + path);
                    return null; // no hierarchy item that corresponds to path parameter was found in the repository
                }
            }
            catch (RequestFailedException ex)
            {
                // token is not valid or access is denied
                if (ex.Status == 401)
                {
                    await HttpContext.SignOutAsync();
                    return null;
                }
                else
                {
                    throw ex;
                }
            }

            IHierarchyItem item;
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
