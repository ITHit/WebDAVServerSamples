using System;
using System.IO;
using System.Threading.Tasks;
using CardDAVServer.FileSystemStorage.AspNetCore.CardDav;

namespace CardDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// This class creates initial calendar(s) and address book(s) for user during first log-in.
    /// </summary>
    /// <remarks>
    /// In case of windows authentication methods in this class are using impersonation. In 
    /// case you run IIS Express and log-in as the user that is different from the one running 
    /// IIS Express, the IIS Express must run with Administrative permissions.
    /// </remarks>
    public class Provisioning
    {

        /// <summary>
        /// Creates initial address books for user.
        /// </summary>
        internal static async Task CreateAddressbookFoldersAsync(DavContext context)
        {
            string physicalRepositoryPath = context.RepositoryPath;

            // Get path to user folder /addrsessbooks/[user_name]/ and check if it exists.
            string addressbooksUserFolder = string.Format("{0}{1}", AddressbooksRootFolder.AddressbooksRootFolderPath.Replace('/', Path.DirectorySeparatorChar), context.UserName);
            string pathAddressbooksUserFolder = Path.Combine(physicalRepositoryPath, addressbooksUserFolder.TrimStart(Path.DirectorySeparatorChar));
            if (!Directory.Exists(pathAddressbooksUserFolder))
            {
                Directory.CreateDirectory(pathAddressbooksUserFolder);

                        // Create user address books, such as /addressbooks/[user_name]/Addressbook/.
                        string pathAddressbook = Path.Combine(pathAddressbooksUserFolder, "Addressbook1");
                        Directory.CreateDirectory(pathAddressbook);
                        pathAddressbook = Path.Combine(pathAddressbooksUserFolder, "Business1");
                        Directory.CreateDirectory(pathAddressbook);
            }
        }
    }
}
