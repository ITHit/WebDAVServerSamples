using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Configuration;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Quota;


namespace WebDAVServer.FileSystemStorage.HttpListener
{
    /// <summary>
    /// Implementation of <see cref="DavContextBaseAsync"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        DavContextHttpListenerBaseAsync
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
        public WebSocketsService socketService { get { return WebSocketsService.Service; } private set { } }

        /// <summary>
        /// Gets user name.
        /// </summary>
        /// <remarks>In case of windows authentication returns user name without domain part.</remarks>
        public string UserName
        {
            get
            {
                int i = Identity.Name.IndexOf("\\");
                return i > 0 ? Identity.Name.Substring(i + 1, Identity.Name.Length - i - 1) : Identity.Name;
            }
        }

        /// <summary>
        /// Gets currently authenticated user.
        /// </summary>

        /// <summary>
        /// Currently logged in identity.
        /// </summary>
        public IIdentity Identity { get; private set; }

        /// <summary>
        /// Initializes a new instance of the DavContext class.
        /// </summary>
        /// <param name="listenerContext"><see cref="HttpListenerContext"/> instance.</param>
        /// <param name="prefixes">Http listener prefixes.</param>
        /// <param name="repositoryPath">Local path to repository.</param>
        /// <param name="logger"><see cref="ILogger"/> instance.</param>
        public DavContext(
            HttpListenerContext listenerContext,
            HttpListenerPrefixCollection prefixes,
            System.Security.Principal.IPrincipal principal,
            string repositoryPath,
            ILogger logger)
            : base(listenerContext, prefixes)
        {
            this.Logger = logger;
            this.RepositoryPath = repositoryPath;
            if (!Directory.Exists(repositoryPath))
            {
                Logger.LogError("Repository path specified in Web.config is invalid.", null);
            }

            if (principal != null)
                Identity = principal.Identity;
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
