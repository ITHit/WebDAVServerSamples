using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;

namespace CardDAVServer.SqlStorage.AspNet.CardDav
{
    public static class CardDavFactory
    {
        /// <summary>
        /// Gets CardDAV items.
        /// </summary>
        /// <param name="path">Relative path requested.</param>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <returns>Object implementing various business card items or null if no object corresponding to path is found.</returns>
        public static async Task<IHierarchyItemAsync> GetCardDavItemAsync(DavContext context, string path)
        {
            // If this is [DAVLocation]/addressbooks - return folder that contains all addressbooks.
            if (path.Equals(AddressbooksRootFolder.AddressbooksRootFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                return new AddressbooksRootFolder(context);
            }

            string[] segments = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            // If URL ends with .vcf - return address book file, which contains vCard.
            if (path.EndsWith(CardFile.Extension, System.StringComparison.InvariantCultureIgnoreCase))
            {
                string fileName = EncodeUtil.DecodeUrlPart(System.IO.Path.GetFileNameWithoutExtension(segments.Last())).Normalize(NormalizationForm.FormC);
                return (await CardFile.LoadByFileNamesAsync(context, new[] { fileName }, PropsToLoad.All)).FirstOrDefault();
            }

            // If this is [DAVLocation]/addressbooks/[AddressbookFolderId]/ return address book.
            if (path.StartsWith(AddressbooksRootFolder.AddressbooksRootFolderPath.Trim('/'), System.StringComparison.InvariantCultureIgnoreCase))
            {
                Guid addressbookFolderId;
                    if (Guid.TryParse(EncodeUtil.DecodeUrlPart(segments.Last()), out addressbookFolderId))
                  
                    {
                        return await AddressbookFolder.LoadByIdAsync(context, addressbookFolderId);
                    }
            }

            return null;
        }
    }
}