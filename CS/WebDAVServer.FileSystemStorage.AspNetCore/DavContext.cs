using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Quota;
using WebDAVServer.FileSystemStorage.AspNetCore.Options;
using WebDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;



namespace WebDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItemAsync}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItemAsync>
    {

        /// <summary>
        /// Path to the folder which become available via WebDAV.
        /// </summary>
        public string RepositoryPath { get; private set; }

        /// <summary>
        /// Gets WebDAV Logger instance.
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Singleton instance of <see cref="WebSocketsService"/>.
        /// </summary>
        public WebSocketsService socketService { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="configOptions">WebDAV Context configuration options.</param>
        /// <param name="logger">WebDAV Logger instance.</param>
        /// <param name="socketService">Singleton instance of <see cref="WebSocketsService"/>.</param>
        public DavContext(IHttpContextAccessor httpContextAccessor, IOptions<DavContextOptions> configOptions, ILogger logger
            , WebSocketsService socketService
            )
            : base(httpContextAccessor.HttpContext)
        {
            RepositoryPath = configOptions.Value.RepositoryPath;
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
        /// Returns the physical file path that corresponds to the specified virtual path on the Web server.
        /// </summary>
        /// <param name="relativePath">Path relative to WebDAV root folder.</param>
        /// <returns>Corresponding path in file system.</returns>
        internal string MapPath(string relativePath)
        {
            //Convert to local file system path by decoding every part, reversing slashes and appending
            //to repository root.
            string[] encodedParts = relativePath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string[] decodedParts = encodedParts.Select<string, string>(EncodeUtil.DecodeUrlPart).ToArray();
            return Path.Combine(RepositoryPath, string.Join(Path.DirectorySeparatorChar.ToString(), decodedParts));
        }
    }
}
