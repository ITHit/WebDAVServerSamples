using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Web;
using System.Configuration;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Quota;
using ILogger = ITHit.Server.ILogger;
using WebDAVServer.FileSystemStorage.AspNet.ExtendedAttributes;



namespace WebDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItemAsync}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextWebAsync<IHierarchyItemAsync>
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
        /// Initializes a new instance of the DavContext class.
        /// </summary>
        /// <param name="httpContext"><see cref="HttpContext"/> instance.</param>
        public DavContext(HttpContext httpContext) : base(httpContext)
        {
            Logger = WebDAVServer.FileSystemStorage.AspNet.Logger.Instance;
            RepositoryPath = ConfigurationManager.AppSettings["RepositoryPath"] ?? string.Empty;
            bool isRoot = new DirectoryInfo(RepositoryPath).Parent == null;
            string configRepositoryPath = isRoot ? RepositoryPath : RepositoryPath.TrimEnd(Path.DirectorySeparatorChar);
            RepositoryPath = configRepositoryPath.StartsWith("~") ?
                HttpContext.Current.Server.MapPath(configRepositoryPath) : configRepositoryPath;

            string attrStoragePath = (ConfigurationManager.AppSettings["AttrStoragePath"] ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar);
            attrStoragePath = attrStoragePath.StartsWith("~") ?
                HttpContext.Current.Server.MapPath(attrStoragePath) : attrStoragePath;

            if (!FileSystemInfoExtension.IsUsingFileSystemAttribute)
            {
                if (!string.IsNullOrEmpty(attrStoragePath))
                {
                    FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(attrStoragePath, this.RepositoryPath));
                } 
                else if (!(new DirectoryInfo(RepositoryPath).IsExtendedAttributesSupported()))
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                    FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(tempPath, this.RepositoryPath));
                }
            }


            if (!Directory.Exists(RepositoryPath))
            {
                WebDAVServer.FileSystemStorage.AspNet.Logger.Instance.LogError("Repository path specified in Web.config is invalid.", null);
            }
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
