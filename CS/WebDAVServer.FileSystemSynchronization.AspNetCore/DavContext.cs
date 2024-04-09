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
using ITHit.WebDAV.Server.Synchronization;
using ITHit.FileSystem.Core;
using ILogger = ITHit.Server.ILogger;
using WebDAVServer.FileSystemSynchronization.AspNetCore.Configuration;
using WebDAVServer.FileSystemSynchronization.AspNetCore.ExtendedAttributes;

namespace WebDAVServer.FileSystemSynchronization.AspNetCore
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItem}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItem>
    {

        /// <summary>
        /// Context configuration
        /// </summary>
        public DavContextConfig Config { get; private set; }
        /// <summary>
        /// Path to the folder which become available via WebDAV.
        /// </summary>
        public string RepositoryPath { get => Config.RepositoryPath; }

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

            IHierarchyItem item = null;

            item = await DavFolder.GetFolderAsync(this, path);
            if (item != null && (item as DavHierarchyItem).ChangeType != Change.Deleted)
                return item;

            item = await DavFile.GetFileAsync(this, path);
            if (item != null && (item as DavHierarchyItem).ChangeType != Change.Deleted)
                return item;

            Logger.LogDebug("Could not find item that corresponds to path: " + path);

            return null; // no hierarchy item that corresponds to path parameter was found in the repository
        }
        /// <summary>
        /// Returns the physical file path that corresponds to the specified virtual path on the Web server.
        /// </summary>
        /// <param name="relativePath">Path relative to WebDAV root folder, can be updated when file/folder id is used in url scheme.</param>
        /// <returns>Corresponding path in file system.</returns>
        internal string MapPath(ref string relativePath)
        {
            //Convert to local file system path by decoding every part, reversing slashes and appending
            //to repository root.
            string[] encodedParts = relativePath.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string[] decodedParts = encodedParts.Select<string, string>(EncodeUtil.DecodeUrlPart).ToArray();

            // check scheme with resource id
            if (relativePath.StartsWith("ID/"))
         
            {
                string fileSystemItemPath = FileSystemItem.GetPathByFileId((new DriveInfo(RepositoryPath)).Name, long.Parse(decodedParts[decodedParts.Length - 1]));
                relativePath = fileSystemItemPath?.Substring(RepositoryPath.Length).Replace(Path.DirectorySeparatorChar.ToString(), "/");

                return fileSystemItemPath;
            }
            else
            {
                return Path.Combine(RepositoryPath, string.Join(Path.DirectorySeparatorChar.ToString(), decodedParts));
            }
        }
    }
}
