using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

using ITHit.WebDAV.Server.CardDav;
using ITHit.Collab;
using ITHit.Collab.Card;


namespace CardDAVServer.FileSystemStorage.AspNet.CardDav
{
    /// <summary>
    /// Represents business card in an address book on a CardDAV server. Typically contains a single business card in vCard format.
    /// Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[user_name]/[addressbook_name]/[file_name].vcf.
    /// </summary>
    /// <example>
    /// [DAVLocation]
    ///  |-- ...
    ///  |-- addressbooks
    ///      |-- ...
    ///      |-- [User2]
    ///           |-- [Address Book 1]
    ///           |-- ...
    ///           |-- [Address Book X]
    ///                |-- [File 1.vcf]  -- this class
    ///                |-- ...
    ///                |-- [File X.vcf]  -- this class
    /// </example>
    public class CardFile : DavFile, ICardFileAsync
    {
        /// <summary>
        /// Returns business card file that corresponds to path.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        /// <returns>CardFile instance or null if not found.</returns>
        public static CardFile GetCardFile(DavContext context, string path)
        {
            string pattern = string.Format(@"^/?{0}/(?<user_name>[^/]+)/(?<addressbook_name>[^/]+)/(?<file_name>[^/]+\.vcf)$",
                              AddressbooksRootFolder.AddressbooksRootFolderPath.Trim(new char[] { '/' }).Replace("/", "/?"));
            if (!Regex.IsMatch(path, pattern))
                return null;

            FileInfo file = new FileInfo(context.MapPath(path));
            if (!file.Exists)
                return null;

            return new CardFile(file, context, path);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CardFile"/> class.
        /// </summary>
        /// <param name="file"><see cref="FileInfo"/> for corresponding object in file system.</param>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="path">Encoded path relative to WebDAV root.</param>
        private CardFile(FileInfo file, DavContext context, string path)
            : base(file, context, path)
        {
        }

        /// <summary>
        /// Called when a card must be read from back-end storage.
        /// </summary>
        /// <param name="output">Stream to write vCard content.</param>
        /// <param name="startIndex">Index to start reading data from back-end storage. Used for segmented reads, not used by CardDAV clients.</param>
        /// <param name="count">Number of bytes to read. Used for segmented reads, not used by CardDAV clients.</param>
        public override async Task ReadAsync(Stream output, long startIndex, long count)
        {
            string vCard = null;
            using (StreamReader reader = File.OpenText(this.fileSystemInfo.FullName))
            {
                vCard = await reader.ReadToEndAsync();
            }

            // Typically the stream contains a single vCard.
            IEnumerable<IComponent> cards = new vFormatter().Deserialize(vCard);
            ICard2 card = cards.First() as ICard2;

            // We store a business card in the original vCard form sent by the CardDAV client app.
            // This form may not be understood by some CardDAV client apps.
            // Here we convert card to the form understood by the client CardDAV application.

            if (AppleCardInteroperability.Convert(context.Request.UserAgent, card))
            {
                // Write modified vCard to output.
                new vFormatter().Serialize(output, card);
            }
            else
            {
                // No conversion is needed.
                await base.ReadAsync(output, startIndex, count);
            }
        }


        /// <summary>
        /// Called when card is being saved to back-end storage.
        /// </summary>
        /// <param name="stream">Stream containing VCARD.</param>
        /// <param name="contentType">Content type.</param>
        /// <param name="startIndex">Starting byte in target file
        /// for which data comes in <paramref name="content"/> stream.</param>
        /// <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        /// <returns>Whether the whole stream has been written.</returns>
        public override async Task<bool> WriteAsync(Stream content, string contentType, long startIndex, long totalFileSize)
        {
            // We store a business card in the original vCard form sent by the CardDAV client app.
            // This form may not be understood by some CardDAV client apps. 
            // We will convert the card if needed when reading depending on the client app reading the vCard.
            return await base.WriteAsync(content, contentType, startIndex, totalFileSize);
        }
    }
}