using System;
using System.IO;
using System.Web;
using System.Configuration;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Threading.Tasks;
using CardDAVServer.FileSystemStorage.AspNet.CardDav;

namespace CardDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    /// <remarks>
    /// In case of windows authentication methods in this class are using impersonation. In 
    /// case you run IIS Express and log-in as the user that is different from the one running 
    /// IIS Express, the IIS Express must run with Administrative permissions.
    /// </remarks>
    public class Provisioning: IHttpModule
    {
        /// <summary>
        /// Path to the folder which stores WebDAV files.
        /// </summary>
        private static readonly string repositoryPath = ConfigurationManager.AppSettings["RepositoryPath"] ?? string.Empty;

        public void Dispose()
        {
        }

        public void Init(HttpApplication application)
        {
            EventHandlerTaskAsyncHelper postAuthAsyncHelper = new EventHandlerTaskAsyncHelper(App_OnPostAuthenticateRequestAsync);
            application.AddOnPostAuthenticateRequestAsync(postAuthAsyncHelper.BeginEventHandler, postAuthAsyncHelper.EndEventHandler);
        }

        private async Task App_OnPostAuthenticateRequestAsync(object source, EventArgs eventArgs)
        {
            HttpContext httpContext = HttpContext.Current;
            if ((httpContext.User == null) || !httpContext.User.Identity.IsAuthenticated)
                return;

            DavContext context = new DavContext(httpContext);

            // Create addressboks for the user during first log-in.
            await CreateAddressbookFoldersAsync(context);
        }

        /// <summary>
        /// Creates initial address books for user.
        /// </summary>
        internal static async Task CreateAddressbookFoldersAsync(DavContext context)
        {
            string physicalRepositoryPath = repositoryPath.StartsWith("~") ? HttpContext.Current.Server.MapPath(repositoryPath) :repositoryPath;

            // Get path to user folder /addrsessbooks/[user_name]/ and check if it exists.
            string addressbooksUserFolder = string.Format("{0}{1}", AddressbooksRootFolder.AddressbooksRootFolderPath.Replace('/', Path.DirectorySeparatorChar), context.UserName);
            string pathAddressbooksUserFolder = Path.Combine(physicalRepositoryPath, addressbooksUserFolder.TrimStart(Path.DirectorySeparatorChar));
            if (!Directory.Exists(pathAddressbooksUserFolder))
            {
                Directory.CreateDirectory(pathAddressbooksUserFolder);

                // Grant full control to loged-in user.
                GrantFullControl(pathAddressbooksUserFolder, context);

                // Create all subfolders under the loged-in user account
                // so all folders has loged-in user as the owner.
                context.FileOperation(
                    () =>
                    {
                        // Make the loged-in user the owner of the new folder.
                        MakeOwner(pathAddressbooksUserFolder, context);

                        // Create user address books, such as /addressbooks/[user_name]/Addressbook/.
                        string pathAddressbook = Path.Combine(pathAddressbooksUserFolder, "Addressbook1");
                        Directory.CreateDirectory(pathAddressbook);
                        pathAddressbook = Path.Combine(pathAddressbooksUserFolder, "Business1");
                        Directory.CreateDirectory(pathAddressbook);
                    });
            }
        }

        /// <summary>
        /// Makes the loged-in user the owner of the folder.
        /// </summary>
        /// <param name="folderPath">folder path in file system</param>
        private static void MakeOwner(string folderPath, DavContext context)
        {
            DirectorySecurity securityOwner = Directory.GetAccessControl(folderPath, AccessControlSections.Owner);
            securityOwner.SetOwner(context.WindowsIdentity.User);
            Directory.SetAccessControl(folderPath, securityOwner);
        }

        /// <summary>
        /// Grants full controll to currently loged-in user.
        /// </summary>
        /// <param name="folderPath">folder path in file system</param>
        private static void GrantFullControl(string folderPath, DavContext context)
        {
            DirectoryInfo folder = new DirectoryInfo(folderPath);
            DirectorySecurity security = folder.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(context.WindowsIdentity.User, FileSystemRights.FullControl, InheritanceFlags.ObjectInherit | InheritanceFlags.ContainerInherit, PropagationFlags.None, AccessControlType.Allow));
            folder.SetAccessControl(security);
        }
    }
}
