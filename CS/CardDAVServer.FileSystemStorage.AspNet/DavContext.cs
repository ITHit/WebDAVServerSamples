using System;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Configuration;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Quota;
using CardDAVServer.FileSystemStorage.AspNet.Acl;
using CardDAVServer.FileSystemStorage.AspNet.CardDav;
using CardDAVServer.FileSystemStorage.AspNet.ExtendedAttributes;



namespace CardDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItemAsync}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextWebAsync<IHierarchyItemAsync>, IDisposable
    {

        /// <summary>
        /// Disk full windows error code.
        /// </summary>
        private const int ERROR_DISK_FULL = 0x70;

        /// <summary>
        /// A <see cref="PrincipalContext"/> to be used for windows users operations.
        /// </summary>
        private PrincipalContext principalContext;

        /// <summary>
        /// Path to the folder which become available via WebDAV.
        /// </summary>
        public string RepositoryPath { get; private set; }

        /// <summary>
        /// Gets WebDAV Logger instance.
        /// </summary>
        public ILogger Logger { get; private set; }

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
        public WindowsIdentity WindowsIdentity
        {
            get
            {
                WindowsIdentity winIdentity = Identity as WindowsIdentity;
                if (winIdentity != null && !winIdentity.IsAnonymous)
                {
                    return winIdentity;
                }

                if (AnonymousUser != null)
                {
                    return AnonymousUser;
                }

                throw new Exception("Anonymous user is not configured.");
            }
        }

        /// <summary>
        /// Currently logged in identity.
        /// </summary>
        public IIdentity Identity { get; private set; }

        /// <summary>
        /// Gets domain of currently logged in user.
        /// </summary>
        public string Domain
        {
            get
            {
                int i = WindowsIdentity.Name.IndexOf("\\");
                return i > 0 ? WindowsIdentity.Name.Substring(0, i) : Environment.MachineName;
            }
        }

        /// <summary>
        /// Gets or sets user configured as anonymous.
        /// </summary>
        public WindowsIdentity AnonymousUser { get; set; }

        /// <summary>
        /// Retrieves or creates <see cref="PrincipalContext"/> to be used for user related operations.
        /// </summary>
        /// <returns>Instance of <see cref="PrincipalContext"/>.</returns>
        public PrincipalContext GetPrincipalContext()
        {
            if (principalContext == null)
            {
                if (string.IsNullOrEmpty(Domain) ||
                    Environment.MachineName.Equals(Domain, StringComparison.InvariantCultureIgnoreCase))
                {
                    principalContext = new PrincipalContext(ContextType.Machine, Domain);
                }
                else
                {
                   principalContext = new PrincipalContext(ContextType.Domain);
                }
            }

            return principalContext;
        }

        /// <summary>
        /// Performs operation which creates, deletes or modifies user or group.
        /// Performs impersonification and exception handling.
        /// </summary>
        /// <param name="action">Action which performs action with a user or group.</param>
        public void PrincipalOperation(Action action)
        {
            using (impersonate())
            {
                try
                {
                    action();
                }
                catch (PrincipalOperationException ex)
                {
                    Logger.LogError("Principal operation failed", ex);
                    throw new DavException("Principal operation failed", ex);
                }
            }
        }

        /// <summary>
        /// Performs operation which queries, creates, deletes or modifies user or group.
        /// Performs impersonification and exception handling.
        /// </summary>
        /// <param name="func">Function to perform.</param>
        /// <typeparam name="T">Type of operation result.</typeparam>
        /// <returns>The value which <paramref name="func"/> returned.</returns>
        public T PrincipalOperation<T>(Func<T> func)
        {
            using (impersonate())
            {
                try
                {
                    return func();
                }
                catch (PrincipalOperationException ex)
                {
                    Logger.LogError("Principal operation failed", ex);
                    throw new DavException("Principal operation failed", ex);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the DavContext class.
        /// </summary>
        /// <param name="httpContext"><see cref="HttpContext"/> instance.</param>
        public DavContext(HttpContext httpContext) : base(httpContext)
        {
            Logger = CardDAVServer.FileSystemStorage.AspNet.Logger.Instance;
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
                CardDAVServer.FileSystemStorage.AspNet.Logger.Instance.LogError("Repository path specified in Web.config is invalid.", null);
            }

            if (httpContext.User != null)
                Identity = httpContext.User.Identity;
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

            // Return items from [DAVLocation]/acl/ folder and subfolders.
            item = await AclFactory.GetAclItemAsync(this, path);
            if (item != null)
                return item;

            // Return items from [DAVLocation]/addressbooks/ folder and subfolders.
            item = CardDavFactory.GetCardDavItem(this, path);
            if (item != null)
                return item;

            // Return folder that corresponds to [DAVLocation] path. If no DavLocation is defined in config file this is a website root.
            item = DavLocationFolder.GetDavLocationFolder(this, path);
            if (item != null)
                return item;

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
            string[] decodedParts = encodedParts.Select<string, string>(p => EncodeUtil.DecodeUrlPart(p).Normalize(NormalizationForm.FormC)).ToArray();
            return Path.Combine(RepositoryPath, string.Join(Path.DirectorySeparatorChar.ToString(), decodedParts));
        }

        /// <summary>
        /// Performs file system operation with translating exceptions to those expected by WebDAV engine.
        /// </summary>
        /// <param name="item">Item on which operation is performed.</param>
        /// <param name="action">The action to be performed.</param>
        /// <param name="privilege">Privilege which is needed to perform the operation. If <see cref="UnauthorizedAccessException"/> is thrown
        /// this method will convert it to <see cref="NeedPrivilegesException"/> exception and specify this privilege in it.</param>
        public void FileOperation(IHierarchyItemAsync item, Action action, Privilege privilege)
        {
            try
            {
                using (impersonate())
                {
                    action();
                }
            }
            catch (UnauthorizedAccessException)
            {
                NeedPrivilegesException ex = new NeedPrivilegesException("Not enough privileges");
                ex.AddRequiredPrivilege(item.Path, privilege);
                throw ex;
            }
            catch (IOException ex)
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr == ERROR_DISK_FULL)
                {
                    throw new InsufficientStorageException();
                }

                throw new DavException(ex.Message, DavStatus.CONFLICT);
            }
        }

        /// <summary>
        /// Performs file system operation with translating exceptions to those expected by WebDAV engine.
        /// </summary>
        /// <param name="item">Item on which operation is performed.</param>
        /// <param name="action">The action to be performed.</param>
        /// <param name="privilege">Privilege which is needed to perform the operation. If <see cref="UnauthorizedAccessException"/> is thrown
        /// this method will convert it to <see cref="NeedPrivilegesException"/> exception and specify this privilege in it.</param>
        public async Task FileOperationAsync(IHierarchyItemAsync item, Func<Task> actionAsync, Privilege privilege)
        {
            try
            {
                using (impersonate())
                {
                   await actionAsync();
                }
            }
            catch (UnauthorizedAccessException)
            {
                NeedPrivilegesException ex = new NeedPrivilegesException("Not enough privileges");
                ex.AddRequiredPrivilege(item.Path, privilege);
                throw ex;
            }
            catch (IOException ex)
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr == ERROR_DISK_FULL)
                {
                    throw new InsufficientStorageException();
                }

                throw new DavException(ex.Message, DavStatus.CONFLICT);
            }
        }

        /// <summary>
        /// Performs file system operation with translating exceptions to those expected by WebDAV engine.
        /// </summary>
        /// <param name="item">Item on which operation is performed.</param>
        /// <param name="func">The action to be performed.</param>
        /// <param name="privilege">Privilege which is needed to perform the operation.
        /// If <see cref="UnauthorizedAccessException"/> is thrown  this method will convert it to
        /// <see cref="NeedPrivilegesException"/> exception and specify this privilege in it.</param>
        /// <typeparam name="T">Type of operation's result.</typeparam>
        /// <returns>Result returned by <paramref name="func"/>.</returns>
        public async Task<T> FileOperationAsync<T>(IHierarchyItemAsync item, Func<Task<T>> actionAsync, Privilege privilege)
        {
            try
            {
                using (impersonate())
                {
                    return await actionAsync();
                }
            }
            catch (UnauthorizedAccessException)
            {
                NeedPrivilegesException ex = new NeedPrivilegesException("Not enough privileges");
                ex.AddRequiredPrivilege(item.Path, privilege);
                throw ex;
            }
            catch (IOException ex)
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr == ERROR_DISK_FULL)
                {
                    throw new InsufficientStorageException();
                }

                throw new DavException(ex.Message, DavStatus.CONFLICT);
            }
        }

        /// <summary>
        /// Performs file system operation with translating exceptions to those expected by WebDAV engine, except
        /// <see cref="UnauthorizedAccessException"/> which must be caught and translated manually.
        /// </summary>        
        /// <param name="action">The action to be performed.</param>
        public async Task FileOperationAsync(Func<Task> actionAsync)
        {
            try
            {
                using (impersonate())
                {
                    await actionAsync();
                }
            }
            catch (IOException ex)
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr == ERROR_DISK_FULL)
                {
                    throw new InsufficientStorageException();
                }

                throw new DavException(ex.Message, DavStatus.CONFLICT);
            }
        }

        /// <summary>
        /// Performs file system operation with translating exceptions to those expected by WebDAV engine, except
        /// <see cref="UnauthorizedAccessException"/> which must be caught and translated manually.
        /// </summary>        
        /// <param name="action">The action to be performed.</param>
        public void FileOperation(Action action)
        {
            try
            {
                using (impersonate())
                {
                    action();
                }
            }
            catch (IOException ex)
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr == ERROR_DISK_FULL)
                {
                    throw new InsufficientStorageException();
                }

                throw new DavException(ex.Message, DavStatus.CONFLICT);
            }
        }

        /// <summary>
        /// Performs file system operation with translating exceptions to those expected by WebDAV engine.
        /// </summary>
        /// <param name="item">Item on which operation is performed.</param>
        /// <param name="func">The action to be performed.</param>
        /// <param name="privilege">Privilege which is needed to perform the operation.
        /// If <see cref="UnauthorizedAccessException"/> is thrown  this method will convert it to
        /// <see cref="NeedPrivilegesException"/> exception and specify this privilege in it.</param>
        /// <typeparam name="T">Type of operation's result.</typeparam>
        /// <returns>Result returned by <paramref name="func"/>.</returns>
        public T FileOperation<T>(IHierarchyItemAsync item, Func<T> func, Privilege privilege)
        {
            try
            {
                using (impersonate())
                {
                    return func();
                }
            }
            catch (UnauthorizedAccessException)
            {
                NeedPrivilegesException ex = new NeedPrivilegesException("Not enough privileges");
                ex.AddRequiredPrivilege(item.Path, privilege);
                throw ex;
            }
            catch (IOException ex)
            {
                int hr = Marshal.GetHRForException(ex);
                if (hr == ERROR_DISK_FULL)
                {
                    throw new InsufficientStorageException();
                }

                throw new DavException(ex.Message, DavStatus.CONFLICT);
            }
        }

        /// <summary>
        /// Dispose everything we have.
        /// </summary>
        public void Dispose()
        {
            if (principalContext != null)
            {
                principalContext.Dispose();
            }
        }

        /// <summary>
        /// Impersonates current user.
        /// </summary>
        /// <returns>Impersonation context, which must be disposed to 'unimpersonate'</returns>
        private WindowsImpersonationContext impersonate()
        {
            return LogonUtil.DuplicateToken(WindowsIdentity).Impersonate();
        }
    }
}
