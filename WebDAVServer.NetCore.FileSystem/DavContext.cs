using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Core;

using WebDAVServer.NetCore.FileSystem.Options;

namespace WebDAVServer.NetCore.FileSystem
{
    /// <summary>
    /// Represents WebDAV Context. Resolves hierarchy items by paths. 
    /// </summary>
    /// <remarks>
    /// A new instance of this class is created in every request and passed to WebDAV Engine Run()/RunAsync() method.
    /// </remarks>
    public class DavContext : DavContextCoreBaseAsync
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
        /// Gets <see cref="HttpContext"/> instance.
        /// </summary>
        public HttpContext HttpContext { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="configOptions">WebDAV Context configuration options.</param>
        /// <param name="logger">WebDAV Logger instance.</param>
        public DavContext(IHttpContextAccessor httpContextAccessor, IOptions<DavContextOptions> configOptions, ILogger logger) 
            : base(httpContextAccessor.HttpContext)
        {
            HttpContext     = httpContextAccessor.HttpContext;
            RepositoryPath  = configOptions.Value.RepositoryPath;
            Logger          = logger;
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
        /// <param name="path">Path relative to WebDAV root folder.</param>
        /// <returns>Corresponding path in file system.</returns>
        internal string MapPath(string path)
        {
            // Convert to local file system path by decoding every part, reversing slashes and appending
            // to repository root.
            string[] encodedParts = path.Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            string[] decodedParts = encodedParts.Select<string, string>(EncodeUtil.DecodeUrlPart).ToArray();
            return Path.Combine(RepositoryPath, string.Join(Path.DirectorySeparatorChar.ToString(), decodedParts));
        }
    }
}
