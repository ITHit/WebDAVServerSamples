using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Acl;
using ITHit.WebDAV.Server.Quota;
using CardDAVServer.FileSystemStorage.AspNetCore.Acl;
using CardDAVServer.FileSystemStorage.AspNetCore.CardDav;
using CardDAVServer.FileSystemStorage.AspNetCore.Configuration;
using CardDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;



namespace CardDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Implementation of <see cref="ContextAsync{IHierarchyItemAsync}"/>.
    /// Resolves hierarchy items by paths.
    /// </summary>
    public class DavContext :
        ContextCoreAsync<IHierarchyItemAsync>
    {

        /// <summary>
        /// Disk full windows error code.
        /// </summary>
        private const int ERROR_DISK_FULL = 0x70;

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
        /// Represents array of users from storage.
        /// </summary>
        internal DavUser[] Users { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="httpContextAccessor">Http context.</param>
        /// <param name="config">WebDAV Context configuration.</param>
        /// <param name="logger">WebDAV Logger instance.</param>
        /// <param name="configUsers">WebDAV Users configuration.</param>
        public DavContext(IHttpContextAccessor httpContextAccessor, IOptions<DavContextConfig> config, ILogger logger
            , IOptions<DavUsersConfig> configUsers
            )
            : base(httpContextAccessor.HttpContext)
        {
            HttpContext httpContext = httpContextAccessor.HttpContext;
            if (httpContext.User != null)
                Identity = httpContext.User.Identity;
            Users = configUsers.Value.Users;
            Config = config.Value;
            Logger = logger;
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
                action();
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
                await actionAsync();
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
                return await actionAsync();
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
                await actionAsync();
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
                action();
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
                return func();
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
    }
}
