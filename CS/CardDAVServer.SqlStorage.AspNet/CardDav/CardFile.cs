using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;


using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;

using ITHit.Collab;
using ITHit.Collab.Card;


namespace CardDAVServer.SqlStorage.AspNet.CardDav
{
    /// <summary>
    /// Represents a vCard file.
    /// Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[AddressbookFolderId]/[FileName].vcf
    /// </summary>
    public class CardFile : DavHierarchyItem, ICardFileAsync
    {
        /// <summary>
        /// Card file extension.
        /// </summary>
        public static string Extension = ".vcf";

        /// <summary>
        /// Loads card files contained in an addressbook folder by address book folder ID.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="addressbookFolderId">Address book for which cards should be loaded.</param>
        /// <param name="propsToLoad">Specifies which properties should be loaded.</param>
        /// <returns>List of <see cref="ICardFileAsync"/> items.</returns>
        public static async Task<IEnumerable<ICardFileAsync>> LoadByAddressbookFolderIdAsync(DavContext context, Guid addressbookFolderId, PropsToLoad propsToLoad)
        {
            // propsToLoad == PropsToLoad.Minimum -> Typical GetChildren call by iOS, Android, eM Client, etc CardDAV clients
            // [Summary] is typically not required in GetChildren call, 
            // they are extracted for demo purposes only, to be displayed in Ajax File Browser.

            // propsToLoad == PropsToLoad.All -> Bynari call, it requires all props in GetChildren call.

            if (propsToLoad != PropsToLoad.Minimum)
                throw new NotImplementedException("LoadByAddressbookFolderIdAsync is implemented only with PropsToLoad.Minimum.");

            string sql = @"SELECT * FROM [card_CardFile] 
                           WHERE [AddressbookFolderId] = @AddressbookFolderId
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)";

            //sql = string.Format(sql, GetScPropsToLoad(propsToLoad));
            
            return await LoadAsync(context, sql,
                  "@UserId"             , context.UserId
                , "@AddressbookFolderId", addressbookFolderId);
        }

        /// <summary>
        /// Loads card files by list of their names.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="fileNames">File names to load.</param>
        /// <param name="propsToLoad">Specifies which properties should be loaded.</param>
        /// <returns>List of <see cref="ICardFileAsync"/> items.</returns>
        public static async Task<IEnumerable<ICardFileAsync>> LoadByFileNamesAsync(DavContext context, IEnumerable<string> fileNames, PropsToLoad propsToLoad)
        {
            // Get IN clause part with list of file UIDs for SELECT.
            string selectIn = string.Join(", ", fileNames.Select(a => string.Format("'{0}'", a)).ToArray());

            string sql = @"SELECT * FROM [card_CardFile] 
                           WHERE [FileName] IN ({0})
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)";

            if(propsToLoad==PropsToLoad.All)
            {
                sql += @";SELECT * FROM [card_Email]             WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_Address]           WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_InstantMessenger]  WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_Telephone]         WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_Url]               WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_CustomProperty]    WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0})) AND [ClientAppName] = @ClientAppName";
            }

            sql = string.Format(sql, selectIn);

            return await LoadAsync(context, sql,
                  "@UserId", context.UserId
                , "@ClientAppName", AppleCardInteroperability.GetClientAppName(context.Request.UserAgent));
        }

        /// <summary>
        /// Loads card files by SQL.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="sql">SQL that queries [card_CardFile], [card_Email], etc tables.</param>
        /// <param name="prms">List of SQL parameters.</param>
        /// <returns>List of <see cref="ICardFileAsync"/> items.</returns>
        private static async Task<IEnumerable<ICardFileAsync>> LoadAsync(DavContext context, string sql, params object[] prms)
        {
            IList<ICardFileAsync> items = new List<ICardFileAsync>();

            Stopwatch stopWatch = Stopwatch.StartNew();

            using (SqlDataReader reader = await context.ExecuteReaderAsync(sql, prms))
            {
                DataTable cards = new DataTable();
                cards.Load(reader);

                DataTable emails = new DataTable();
                if (!reader.IsClosed)
                    emails.Load(reader);

                DataTable addresses = new DataTable();
                if (!reader.IsClosed)
                    addresses.Load(reader);

                DataTable instantMessengers = new DataTable();
                if (!reader.IsClosed)
                    instantMessengers.Load(reader);

                DataTable telephones = new DataTable();
                if (!reader.IsClosed)
                    telephones.Load(reader);

                DataTable urls = new DataTable();
                if (!reader.IsClosed)
                    urls.Load(reader);

                DataTable cardCustomProperties = new DataTable();
                if (!reader.IsClosed)
                    cardCustomProperties.Load(reader);

                stopWatch.Stop();
                context.Engine.Logger.LogDebug(string.Format("SQL took: {0}ms", stopWatch.ElapsedMilliseconds));


                foreach (DataRow rowCardFile in cards.Rows)
                {
                    DataRow[] rowsEmails                = new DataRow[0];
                    DataRow[] rowsAddresses             = new DataRow[0];
                    DataRow[] rowsInstantMessengers     = new DataRow[0];
                    DataRow[] rowsTelephones            = new DataRow[0];
                    DataRow[] rowsUrls                  = new DataRow[0];
                    DataRow[] rowsCustomProperties      = new DataRow[0];

                    string uid = rowCardFile.Field<string>("UID");

                    if (emails.Columns["UID"] != null)
                    {
                        string filter = string.Format("UID = '{0}'", uid);

                        rowsEmails              = emails.Select(filter);
                        rowsAddresses           = addresses.Select(filter);
                        rowsInstantMessengers   = instantMessengers.Select(filter);
                        rowsTelephones          = telephones.Select(filter);
                        rowsUrls                = urls.Select(filter);
                        rowsCustomProperties = cardCustomProperties.Select(filter);
                    }

                    string fileName = rowCardFile.Field<string>("FileName");

                    items.Add(new CardFile(context, fileName, rowCardFile, rowsEmails, rowsAddresses, rowsInstantMessengers, rowsTelephones, rowsUrls, rowsCustomProperties));
                }
            }

            return items;
        }

        /// <summary>
        /// Creates a new card file. The actual new [card_CardFile], [card_Email], etc. records are inserted into the database during <see cref="WriteAsync"/> method call.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param> 
        /// <param name="addressbookFolderId">Address book folder ID to which this card file will belong to.</param>
        /// <param name="fileName">New card file name.</param>
        /// <returns>Instance of <see cref="CardFile"/>.</returns>
        public static CardFile CreateCardFile(DavContext context, Guid addressbookFolderId, string fileName)
        {
            CardFile cardFile = new CardFile(context, fileName, null, null, null, null, null, null, null);
            cardFile.addressbookFolderId = addressbookFolderId;
            return cardFile;
        }

        /// <summary>
        /// This card file name.
        /// </summary>
        private readonly string fileName = null;

        /// <summary>
        /// Contains data from [card_CardFile] table.
        /// </summary>
        private readonly DataRow rowCardFile = null;

        /// <summary>
        /// Contains e-mails for this card from [card_Email] table.
        /// </summary>
        private readonly DataRow[] rowsEmails = null;

        /// <summary>
        /// Contains addresses for this card from [card_Address] table.
        /// </summary>
        private readonly DataRow[] rowsAddresses = null;

        /// <summary>
        /// Contains instant messengers for this card from [card_InstantMessenger] table.
        /// </summary>
        private readonly DataRow[] rowsInstantMessengers = null;

        /// <summary>
        /// Contains telephones for this card from [card_Telephone] table.
        /// </summary>
        private readonly DataRow[] rowsTelephones = null;

        /// <summary>
        /// Contains URLs for this card from [card_Url] table.
        /// </summary>
        private readonly DataRow[] rowsUrls = null;

        /// <summary>
        /// Contains custom properties and custom parameters for this card, it's 
        /// e-mails, addresses, instant messengers or telephones form [card_CustomProperty] table.
        /// </summary>
        private readonly DataRow[] rowsCustomProperties = null;

        /// <summary>
        /// Indicates if this is a newly created card.
        /// </summary>
        private bool isNew
        {
            get { return addressbookFolderId != Guid.Empty; }
        }

        /// <summary>
        /// Used to form unique SQL parameter names.
        /// </summary>
        private int paramIndex = 0;

        /// <summary>
        /// Addressbook folder ID in which the new card will be created.
        /// </summary>
        private Guid addressbookFolderId = Guid.Empty;

        /// <summary>
        /// Gets display name of the card. Used for demo purposes only, to be displayed in Ajax File Browser.
        /// </summary>
        /// <remarks>CardDAV clients typically never request this property.</remarks>
        public override string Name
        {
            get
            {
                return rowCardFile.Field<string>("FormattedName");
            }
        }

        /// <summary>
        /// Gets item path.
        /// </summary>
        /// <remarks>[DAVLocation]/addressbooks/[AddressbookFolderId]/[FileName].vcf</remarks>
        public override string Path
        {
            get
            {
                Guid addressbookFolderId = rowCardFile.Field<Guid>("AddressbookFolderId");
                string fileName = rowCardFile.Field<string>("FileName");
                return string.Format("{0}{1}/{2}{3}", AddressbooksRootFolder.AddressbooksRootFolderPath, addressbookFolderId, fileName, Extension);
            }
        }

        /// <summary>
        /// Gets eTag. ETag must change every time this card is updated.
        /// </summary>
        public string Etag
        {
            get
            {
                byte[] bETag = rowCardFile.Field<byte[]>("ETag");
                return BitConverter.ToUInt64(bETag.Reverse().ToArray(), 0).ToString(); // convert timestamp value to number
            }
        }

        /// <summary>
        /// Gets item creation date. Must be in UTC.
        /// </summary>
        public override DateTime Created
        {
            get { return rowCardFile.Field<DateTime>("CreatedUtc"); }
        }

        /// <summary>
        /// Gets item modification date. Must be in UTC.
        /// </summary>
        public override DateTime Modified
        {
            get { return rowCardFile.Field<DateTime>("ModifiedUtc"); }
        }

        /// <summary>
        /// File content length. Typicaly never requested by CardDAV clients.
        /// </summary>
        /// <remarks>
        /// If -1 is returned the chunked response will be generated if possible. The getcontentlength property will not be generated.
        /// </remarks>
        public long ContentLength
        {
            get { return -1; }
        }

        /// <summary>
        /// File Mime-type/Content-Type.
        /// </summary>
        public string ContentType
        {
	        get { return "text/vcard"; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CardFile"/> class from database source. 
        /// Used when listing folder content and during multi-get requests.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/></param>
        /// <param name="fileName">Card file name. Unlike with CalDAV, in case of CardDAV this is different from UID.</param>
        /// <param name="rowCardFile">Card file info from [card_CardFile] table.</param>
        /// <param name="rowsEmails">List of e-mails for this card from [card_Email] table.</param>
        /// <param name="rowsAddresses">List of addresses for this card from [card_Address] table.</param>
        /// <param name="rowsInstantMessengers">List of instant messengers for this card from [card_InstantMessenger] table.</param>
        /// <param name="rowsTelephones">List of telephones for this card from [card_Telephone] table.</param>
        /// <param name="rowsUrls">List of URLs for this card from [card_Url] table.</param>
        /// <param name="rowsCustomProperties">List of vCard custom properties and parameters for this card from [card_CustomProperty] table.</param>
        private CardFile(DavContext context, string fileName,
            DataRow rowCardFile, DataRow[] rowsEmails, DataRow[] rowsAddresses, DataRow[] rowsInstantMessengers,
            DataRow[] rowsTelephones, DataRow[] rowsUrls, DataRow[] rowsCustomProperties)
            : base(context)
        {
            this.fileName               = fileName;
            this.rowCardFile            = rowCardFile;
            this.rowsEmails             = rowsEmails;
            this.rowsAddresses          = rowsAddresses;
            this.rowsInstantMessengers  = rowsInstantMessengers;
            this.rowsTelephones         = rowsTelephones;
            this.rowsUrls               = rowsUrls;
            this.rowsCustomProperties   = rowsCustomProperties;
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
        public async Task<bool> WriteAsync(Stream stream, string contentType, long startIndex, long totalFileSize)
        {
            //Set timeout to maximum value to be able to upload large card files.
            System.Web.HttpContext.Current.Server.ScriptTimeout = int.MaxValue;
            string vCard;
            using (StreamReader reader = new StreamReader(stream))
            {
                vCard = reader.ReadToEnd();
            }

            // Typically the stream contains a single vCard.
            IEnumerable<IComponent> cards = new vFormatter().Deserialize(vCard);
            ICard2 card = cards.First() as ICard2;

            // Card file UID which is equal to file name.
            string uid = card.Uid.Text;

            // Check if this CardDAV client application requires properties conversion.
            if (AppleCardInteroperability.NeedsConversion(Context.Request.UserAgent))
            {
                /// Replace "itemX.PROP" properties created by iOS and OS X with "PROP", so they 
                /// are saved as normal props to storage and can be read by any CardDAV client.
                AppleCardInteroperability.Normalize(card);
            }

            // The client app name is stored in DB to update and extract only custom props created by the client making a request.
            string clientAppName = AppleCardInteroperability.GetClientAppName(Context.Request.UserAgent);

            // Save data to [card_CardFile] table.
            await WriteCardFileAsync(Context, card, addressbookFolderId, isNew, clientAppName);

            // Save emails.
            await WriteEmailsAsync(Context, card.Emails, uid, clientAppName);

            // Save addresses.
            await WriteAddressesAsync(Context, card.Addresses, uid, clientAppName);

            // Save telephones.
            await WriteTelephonesAsync(Context, card.Telephones, uid, clientAppName);

            // Save URLs
            await WriteUrlsAsync(Context, card.Urls, uid, clientAppName);

            // Save instant messengers. vCard 3.0+ only
            ICard3 card3 = card as ICard3;
            if (card3 != null)
            {
                await WriteInstantMessengersAsync(Context, card3.InstantMessengers, uid, clientAppName);
            }

            return true;
        }

        /// <summary>
        /// Saves data to [card_CardFile] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="card">Card to read data from.</param>
        /// <param name="addressbookFolderId">Address book folder that contains this file.</param>
        /// <param name="isNew">Flag indicating if this is a new file or file should be updated.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        /// <remarks>
        /// This function deletes records in [card_Email], [card_Address], [card_InstantMessenger],
        /// [card_Telephone], [card_Url] tables if the card should be updated. Values from the [card_CustomProperty] table 
        /// is being deleted if updated by the same client that created a specific custom property.
        /// </remarks>
        private async Task WriteCardFileAsync(DavContext context, ICard2 card, Guid addressbookFolderId, bool isNew, string clientAppName)
        {
            string sql;
            if (isNew)
            {
                sql =
                    @"IF EXISTS (SELECT 1 FROM [card_Access] WHERE [AddressbookFolderId]=@AddressbookFolderId AND [UserId]=@UserId AND [Write]=1)
                      INSERT INTO [card_CardFile] (
                          [UID]
                        , [AddressbookFolderId]
	                    , [FileName]
	                    , [Version]
	                    , [Product]
	                    , [FormattedName]
	                    , [FamilyName]
	                    , [GivenName]
	                    , [AdditionalNames]
	                    , [HonorificPrefix]
	                    , [HonorificSuffix]
	                    , [Kind]
	                    , [Nickname]
	                    , [Photo]
	                    , [PhotoMediaType]
	                    , [Logo]
	                    , [LogoMediaType]
	                    , [Sound]
	                    , [SoundMediaType]
	                    , [Birthday]
	                    , [Anniversary]
	                    , [Gender]
	                    , [RevisionUtc]
	                    , [SortString]
	                    , [Language]
	                    , [TimeZone]
	                    , [Geo]
	                    , [Title]
	                    , [Role]
	                    , [OrgName]
	                    , [OrgUnit]
	                    , [Categories]
	                    , [Note]
	                    , [Classification]
                    ) VALUES (
                          @UID
                        , @AddressbookFolderId
                        , @FileName
                        , @Version
                        , @Product
	                    , @FormattedName
	                    , @FamilyName
	                    , @GivenName
	                    , @AdditionalNames
	                    , @HonorificPrefix
	                    , @HonorificSuffix
	                    , @Kind
	                    , @Nickname
	                    , @Photo
	                    , @PhotoMediaType
	                    , @Logo
	                    , @LogoMediaType
	                    , @Sound
	                    , @SoundMediaType
	                    , @Birthday
	                    , @Anniversary
	                    , @Gender
	                    , @RevisionUtc
	                    , @SortString
	                    , @Language
	                    , @TimeZone
	                    , @Geo
	                    , @Title
	                    , @Role
	                    , @OrgName
	                    , @OrgUnit
	                    , @Categories
	                    , @Note
	                    , @Classification
                    )";
            }
            else
            {
                // We can only update record in [card_CardFile] table.
                // There is no way to update [card_Email], [card_Address], [card_InstantMessenger], [card_Telephone] and [card_Url]
                // tables for existing card, we must delete all records for this UID and recreate.

                // We can keep custom props in [card_CustomProperty] table if they were created by a different CardDAV client.

                // [ModifiedUtc] field update triggers [ETag] field update which is used for synchronyzation.
                sql =
                    @"IF EXISTS (SELECT 1 FROM [card_CardFile] 
                           WHERE FileName=@FileName
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Write] = 1))
                      BEGIN
                          UPDATE [card_CardFile] SET 
                              [ModifiedUtc]     = @ModifiedUtc
	                        , [Version]         = @Version
	                        , [Product]         = @Product
	                        , [FormattedName]   = @FormattedName
	                        , [FamilyName]      = @FamilyName
	                        , [GivenName]       = @GivenName
	                        , [AdditionalNames] = @AdditionalNames
	                        , [HonorificPrefix] = @HonorificPrefix
	                        , [HonorificSuffix] = @HonorificSuffix
	                        , [Kind]            = @Kind
	                        , [Nickname]        = @Nickname
	                        , [Photo]           = @Photo
	                        , [PhotoMediaType]  = @PhotoMediaType
	                        , [Logo]            = @Logo
	                        , [LogoMediaType]   = @LogoMediaType
	                        , [Sound]           = @Sound
	                        , [SoundMediaType]  = @SoundMediaType
	                        , [Birthday]        = @Birthday
	                        , [Anniversary]     = @Anniversary
	                        , [Gender]          = @Gender
	                        , [RevisionUtc]     = @RevisionUtc
	                        , [SortString]      = @SortString
	                        , [Language]        = @Language
	                        , [TimeZone]        = @TimeZone
	                        , [Geo]             = @Geo
	                        , [Title]           = @Title
	                        , [Role]            = @Role
	                        , [OrgName]         = @OrgName
	                        , [OrgUnit]         = @OrgUnit
	                        , [Categories]      = @Categories
	                        , [Note]            = @Note
	                        , [Classification]  = @Classification
                          WHERE 
                            [UID] = @UID
                    
                          ; DELETE FROM [card_Email]              WHERE [UID] = @UID
                          ; DELETE FROM [card_Address]            WHERE [UID] = @UID
                          ; DELETE FROM [card_InstantMessenger]   WHERE [UID] = @UID
                          ; DELETE FROM [card_Telephone]          WHERE [UID] = @UID
                          ; DELETE FROM [card_Url]                WHERE [UID] = @UID
                          ; DELETE FROM [card_CustomProperty]     WHERE [UID] = @UID AND (([ClientAppName] = @ClientAppName) OR ([ParentId] != [UID]) OR ([ClientAppName] IS NULL))
                      END";

            }
            // [ClientAppName] = @ClientAppName -> delete all custom props created by this client.
            // [ParentId] != [UID]              -> delete all custom params from multiple props: EMAIL, ADR, TEL, IMPP. Keep custom params for any single props in [card_Card].
            // [ClientAppName] IS NULL          -> delete all custom props created by some unknown CardDAV client.

            string uid = card.Uid.Text;
                                                                                                        
            if (await context.ExecuteNonQueryAsync(sql,
                  "@UID"            , uid                                                                   // UID
                , "UserId"          , context.UserId
                , "@AddressbookFolderId", addressbookFolderId                                               // used only when inserting
                , "@FileName"       , fileName                                                              // In case of CardDAV a file name is sypically a GUID, but it is different from UID.
                , "@ModifiedUtc"    , DateTime.UtcNow
                , "@Version"        , card.Version.Text                                                     // VERSION
                , "@Product"        , card.ProductId?.Text                                                  // PRODID
                , "@FormattedName"  , card.FormattedNames.PreferedOrFirstProperty.Text                      // FN                           Here we assume only 1 prop for the sake of simplicity.
                , "@FamilyName"     , card.Name.FamilyName                                                  // N
                , "@GivenName"      , card.Name.GivenName                                                   // N
                , "@AdditionalNames", card.Name.AdditionalNamesList                                         // N
                , "@HonorificPrefix", card.Name.HonorificPrefix                                             // N
                , "@HonorificSuffix", card.Name.HonorificSuffix                                             // N
                , "@Kind"           , (card as ICard4)?.Kind?.Text                                          // KIND         (vCard 4.0)
                , "@Nickname"       , (card as ICard3)?.NickNames.PreferedOrFirstProperty?.Values.First()   // NICKNAME     (vCard 3.0+)    Here we assume only 1 prop with 1 value for the sake of simplicity.
                , CreateVarBinaryParam("@Photo", card.Photos.PreferedOrFirstProperty?.Base64Data)           // PHOTO                        Here we assume only 1 prop for the sake of simplicity.
                , "@PhotoMediaType" , card.Photos.PreferedOrFirstProperty?.MediaType                        // PHOTO TYPE param
                , CreateVarBinaryParam("@Logo",  card.Logos.PreferedOrFirstProperty?.Base64Data)            // LOGO                         Here we assume only 1 prop for the sake of simplicity.
                , "@LogoMediaType"  , card.Logos.PreferedOrFirstProperty?.MediaType                         // LOGO  TYPE param
                , CreateVarBinaryParam("@Sound", card.Sounds.PreferedOrFirstProperty?.Base64Data)           // SOUND                        Here we assume only 1 prop for the sake of simplicity.
                , "@SoundMediaType" , card.Sounds.PreferedOrFirstProperty?.MediaType                        // SOUND TYPE param
                , new SqlParameter("@Birthday"   , card.BirthDate?.Value?.DateVal ?? DBNull.Value as object)                { SqlDbType = SqlDbType.DateTime2 }   // BDAY
                , new SqlParameter("@Anniversary", (card as ICard4)?.Anniversary?.Value?.DateVal ?? DBNull.Value as object) { SqlDbType = SqlDbType.DateTime2 }   // ANNIVERSARY  (vCard 4.0)
                , "@Gender"         , (card as ICard4)?.Gender?.Sex                                         // GENDER       (vCard 4.0)
                , "@RevisionUtc"    , card.Revision?.Value.DateVal                                          // REV
                , "@SortString"     , card.SortString?.Text                                                 // SORT-STRING
                , "@Language"       , (card as ICard4)?.ContactLanguages.PreferedOrFirstProperty.Text       // LANG         (vCard 4.0)     Here we assume only 1 prop for the sake of simplicity.
                , "@TimeZone"       , card.TimeZones.PreferedOrFirstProperty?.Text                          // TZ
                , "@Geo"            , null                                                                  // GEO
                , "@Title"          , card.Titles.PreferedOrFirstProperty?.Text                             // TITLE
                , "@Role"           , card.Roles.PreferedOrFirstProperty?.Text                              // ROLE
                , "@OrgName"        , card.Organizations.PreferedOrFirstProperty?.Name                      // ORG                          Here we assume only 1 prop for the sake of simplicity.
                , "@OrgUnit"        , card.Organizations.PreferedOrFirstProperty?.Units?.First()            // ORG                          Here we assume only 1 prop with 1 unit value for the sake of simplicity.
                , "@Categories"     , ListToString<string>((card as ICard3)?.Categories.Select(x => ListToString<string>(x.Values, ",")), ";") // CATEGORIES  (vCard 3.0+)
                , "@Note"           , card.Notes.PreferedOrFirstProperty?.Text                              // NOTE                         Here we assume only 1 prop for the sake of simplicity.
                , "@Classification" , (card as ICard3)?.Classes.PreferedOrFirstProperty?.Text               // CLASS                        Here we assume only 1 prop for the sake of simplicity.
                , "@ClientAppName"  , clientAppName                                                         // Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.
                ) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }

            // Save custom properties and parameters of this component to [card_CustomProperty] table.
            string customPropsSqlInsert;
            List<object> customPropsParamsInsert;
            if (PrepareSqlCustomPropertiesOfComponentAsync(card, uid, uid, clientAppName, out customPropsSqlInsert, out customPropsParamsInsert))
            {
                await context.ExecuteNonQueryAsync(customPropsSqlInsert, customPropsParamsInsert.ToArray());
            }
        }

        /// <summary>
        /// Converts <see cref="IEnumerable{T}"/> to string. Returns null if the list is empty.
        /// </summary>
        /// <returns>String that contains elements separated by ','.</returns>
        private static string ListToString<T>(IEnumerable<T> arr, string separator = ",")
        {
            if ((arr == null) || !arr.Any())
                return null;
            return string.Join<T>(separator, arr);
        }

        /// <summary>
        /// Creates a new SqlParameter with VARBINARY type and base64 content.
        /// </summary>
        /// <param name="parameterName">SQL parameter name.</param>
        /// <param name="base64">Base 64-encoded parameter value.</param>
        /// <returns></returns>
        private static SqlParameter CreateVarBinaryParam(string parameterName, string base64)
        {
            SqlParameter param = new SqlParameter(parameterName, SqlDbType.VarBinary);
            if (string.IsNullOrEmpty(base64))
            {
                // To insert NULL to VARBINARY column, SqlParameter must be passed with Size=-1 and Value=DBNull.Value.
                param.Size      = -1;
                param.Value     = DBNull.Value;
            }
            else
            {
                byte[] content  = Convert.FromBase64String(base64);
                param.Size      = content.Length;
                param.Value     = content;
            }
            return param;
        }

        /// <summary>
        /// Saves data to [card_Email] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="emails">List of emails to be saved.</param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        private async Task WriteEmailsAsync(DavContext context, ITextPropertyList<IEmail2> emails, string uid, string clientAppName)
        {
            string sql =
                @"INSERT INTO [card_Email] (
                      [EmailId]
                    , [UID]
                    , [Type]
                    , [Email]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                "@UID", uid
            });

            int i = 0;
            foreach (IEmail2 email in emails)
            {
                valuesSql.Add(string.Format(@"(
                      @EmailId{0}
                    , @UID
                    , @Type{0}
                    , @Email{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i));

                Guid emailId = Guid.NewGuid();

                parameters.AddRange(new object[] {
                      "@EmailId"            +i, emailId
                  //, "@UID"
                    , "@Type"               +i, ListToString<EmailType>(email.Types)    // EMAIL TYPE param
                    , "@Email"              +i, email.Text                              // EMAIL VALUE
                    , "@PreferenceLevel"    +i, GetPrefParameter(email)                 // EMAIL PREF param
                    , "@SortIndex"          +i, email.RawProperty.SortIndex             // Property position in vCard.
                });

                // Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("EMAIL", email.RawProperty, emailId.ToString(), uid, clientAppName, out customPropSqlInsert, out customPropParametersInsert))
                {
                    sql += "; " + customPropSqlInsert;
                    parameters.AddRange(customPropParametersInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [card_Address] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="addresses">List of addresses to be saved.</param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        private async Task WriteAddressesAsync(DavContext context, ICardPropertyList<IAddress2> addresses, string uid, string clientAppName)
        {
            string sql =
                @"INSERT INTO [card_Address] (
                      [AddressId]
                    , [UID]
                    , [Type]
                    , [PoBox]
                    , [AppartmentNumber]
                    , [Street]
                    , [Locality]
                    , [Region]
                    , [PostalCode]
                    , [Country]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                "@UID", uid
            });

            int i = 0;
            foreach (IAddress2 address in addresses)
            {
                valuesSql.Add(string.Format(@"(
                      @AddressId{0}
                    , @UID
                    , @Type{0}
                    , @PoBox{0}
                    , @AppartmentNumber{0}
                    , @Street{0}
                    , @Locality{0}
                    , @Region{0}
                    , @PostalCode{0}
                    , @Country{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i));

                Guid addressId = Guid.NewGuid();

                parameters.AddRange(new object[] {
                      "@AddressId"            +i, addressId
                  //, "@UID"
                    , "@Type"               +i, ListToString<AddressType>(address.Types)    // ADR TYPE param
#pragma warning disable 0618
                    , "@PoBox"              +i, address.PoBox.FirstOrDefault()
                    , "@AppartmentNumber"   +i, address.AppartmentNumber.FirstOrDefault()
#pragma warning restore 0618
                    , "@Street"             +i, address.Street.FirstOrDefault()
                    , "@Locality"           +i, address.Locality.FirstOrDefault()
                    , "@Region"             +i, address.Region.FirstOrDefault()
                    , "@PostalCode"         +i, address.PostalCode.FirstOrDefault()
                    , "@Country"            +i, address.Country.FirstOrDefault()
                    , "@PreferenceLevel"    +i, GetPrefParameter(address)                   // ADR PREF param
                    , "@SortIndex"          +i, address.RawProperty.SortIndex               // Property position in vCard.
                });

                // Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("ADR", address.RawProperty, addressId.ToString(), uid, clientAppName, out customPropSqlInsert, out customPropParametersInsert))
                {
                    sql += "; " + customPropSqlInsert;
                    parameters.AddRange(customPropParametersInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [card_InstantMessenger] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="instantMessengers">List of instant messengers to be saved.</param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        private async Task WriteInstantMessengersAsync(DavContext context, ITextPropertyList<IInstantMessenger3> instantMessengers, string uid, string clientAppName)
        {
            string sql =
                @"INSERT INTO [card_InstantMessenger] (
                      [InstantMessengerId]
                    , [UID]
                    , [Type]
                    , [InstantMessenger]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                "@UID", uid
            });

            int i = 0;
            foreach (IInstantMessenger3 instantMessenger in instantMessengers)
            {
                valuesSql.Add(string.Format(@"(
                      @InstantMessengerId{0}
                    , @UID
                    , @Type{0}
                    , @InstantMessenger{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i));

                Guid instantMessengerId = Guid.NewGuid();

                parameters.AddRange(new object[] {
                      "@InstantMessengerId" +i, instantMessengerId
                  //, "@UID"
                    , "@Type"               +i, ListToString<MessengerType>(instantMessenger.Types) // IMPP TYPE param
                    , "@InstantMessenger"   +i, instantMessenger.Text                               // IMPP VALUE
                    , "@PreferenceLevel"    +i, GetPrefParameter(instantMessenger)                  // IMPP PREF param
                    , "@SortIndex"          +i, instantMessenger.RawProperty.SortIndex              // Property position in vCard.
                });

                // Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("IMPP", instantMessenger.RawProperty, instantMessengerId.ToString(), uid, clientAppName, out customPropSqlInsert, out customPropParametersInsert))
                {
                    sql += "; " + customPropSqlInsert;
                    parameters.AddRange(customPropParametersInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [card_Telephone] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="instantMessengers">List of telephones to be saved.</param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        private async Task WriteTelephonesAsync(DavContext context, ITextPropertyList<ITelephone2> telephones, string uid, string clientAppName)
        {
            string sql =
                @"INSERT INTO [card_Telephone] (
                      [TelephoneId]
                    , [UID]
                    , [Type]
                    , [Telephone]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                "@UID", uid
            });

            int i = 0;
            foreach (ITelephone2 telephone in telephones)
            {
                valuesSql.Add(string.Format(@"(
                      @TelephoneId{0}
                    , @UID
                    , @Type{0}
                    , @Telephone{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i));

                Guid telephoneId = Guid.NewGuid();

                parameters.AddRange(new object[] {
                      "@TelephoneId"        +i, telephoneId
                  //, "@UID"
                    , "@Type"               +i, ListToString<TelephoneType>(telephone.Types)// TEL TYPE param
                    , "@Telephone"          +i, telephone.Text                              // TEL VALUE
                    , "@PreferenceLevel"    +i, GetPrefParameter(telephone)                 // TEL PREF param
                    , "@SortIndex"          +i, telephone.RawProperty.SortIndex             // Property position in vCard.
                });

                // Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("TEL", telephone.RawProperty, telephoneId.ToString(), uid, clientAppName, out customPropSqlInsert, out customPropParametersInsert))
                {
                    sql += "; " + customPropSqlInsert;
                    parameters.AddRange(customPropParametersInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Saves data to [card_Url] table.
        /// </summary>
        /// <param name="context">Instance of <see cref="DavContext"/> class.</param>
        /// <param name="url">List of URLs to be saved.</param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        private async Task WriteUrlsAsync(DavContext context, ICardPropertyList<ICardUriProperty2> urls, string uid, string clientAppName)
        {
            string sql =
                @"INSERT INTO [card_Url] (
                      [UrlId]
                    , [UID]
                    , [Type]
                    , [Url]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            List<object> parameters = new List<object>(new object[] {
                "@UID", uid
            });

            int i = 0;
            foreach (ICardUriProperty2 url in urls)
            {
                valuesSql.Add(string.Format(@"(
                      @UrlId{0}
                    , @UID
                    , @Type{0}
                    , @Url{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i));

                Guid urlId = Guid.NewGuid();

                parameters.AddRange(new object[] {
                      "@UrlId"              +i, urlId
                  //, "@UID"
                    , "@Type"               +i, ListToString<ExtendibleEnum>(url.Types) // TEL TYPE param 
                    , "@Url"                +i, url.Text                                // URL VALUE
                    , "@PreferenceLevel"    +i, GetPrefParameter(url)                   // URL PREF param
                    , "@SortIndex"          +i, url.RawProperty.SortIndex               // Property position in vCard.
                });

                // Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                string customPropSqlInsert;
                List<object> customPropParametersInsert;
                if (PrepareSqlParamsWriteCustomProperty("URL", url.RawProperty, urlId.ToString(), uid, clientAppName, out customPropSqlInsert, out customPropParametersInsert))
                {
                    sql += "; " + customPropSqlInsert;
                    parameters.AddRange(customPropParametersInsert);
                }

                i++;
            }

            if (i > 0)
            {
                await context.ExecuteNonQueryAsync(string.Format(sql, string.Join(", ", valuesSql.ToArray())), parameters.ToArray());
            }
        }

        /// <summary>
        /// Gets 1 if property is preferred in case of vCard 2.1 & 3.0. Returns preference level in case of vCard 4.0.
        /// </summary>
        /// <param name="prop">Card property to get prefference from.</param>
        /// <returns>Integer between 1 and 100 or null if PREF property is not specified.</returns>
        private static byte? GetPrefParameter(ICardMultiProperty prop)
        {
            ICardMultiProperty4 prop4 = prop as ICardMultiProperty4;
            if(prop4 == null)
            {
                return prop.IsPrefered ? (byte?)1 : null;
            }

            return (byte?)prop4.PreferenceLevel;
        }

        /// <summary>
        /// Creates SQL to write custom properties and parameters to [card_CustomProperty] table.
        /// </summary>
        /// <param name="prop">Raw property to be saved to database.</param>
        /// <param name="parentId">
        /// Parent component ID or parent property ID to which this custom property or parameter belongs to. 
        /// This could be UID (in case of [card_CardFile] table), EmailId, InstantMessengerId, etc.
        /// </param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        /// <param name="sql">SQL to insert data to DB.</param>
        /// <param name="parameters">SQL parameter values that will be filled by this method.</param>
        /// <returns>True if any custom properies or parameters found, false otherwise.</returns>
        private bool PrepareSqlParamsWriteCustomProperty(string propName, IRawProperty prop, string parentId, string uid, string clientAppName, out string sql, out List<object> parameters)
        {
            sql =
                @"INSERT INTO [card_CustomProperty] (
                      [ParentId]
                    , [UID]
                    , [ClientAppName]
                    , [PropertyName]
                    , [ParameterName]
                    , [Value]
                    , [SortIndex]
                ) VALUES {0}";

            List<string> valuesSql = new List<string>();
            parameters = new List<object>();

            int origParamsCount = parameters.Count();

            // Custom properties are one of the following:
            //  - props that start with "X-". This is a standard-based approach to creating custom props.
            //  - props that has "." in its name. Typically "item1.X-PROP". Such props are created by iOS and OS X.
            bool isCustomProp =
                propName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase)
                || propName.Contains(".");


            string paramName = null;

            // Save custom prop value.
            if (isCustomProp)
            {
                string val = prop.RawValue;
                valuesSql.Add(string.Format(@"(
                                  @ParentId{0}
                                , @UID{0}
                                , @ClientAppName{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                , @SortIndexParam{0}
                                )", paramIndex));

                parameters.AddRange(new object[] {
                                  "@ParentId"       + paramIndex, parentId
                                , "@UID"            + paramIndex, uid              // Added for performance optimization purposes.
                                , "@ClientAppName"  + paramIndex, clientAppName    // Client app name that created this custom property.
                                , "@PropertyName"   + paramIndex, propName
                                , "@ParameterName"  + paramIndex, paramName        // null is inserted to mark prop value.
                                , "@Value"          + paramIndex, val
                                , "@SortIndexParam" + paramIndex, prop.SortIndex   // Property position in vCard.
                                });
                paramIndex++;
            }

            // Save parameters and their values.
            foreach (Parameter param in prop.Parameters)
            {
                paramName = param.Name;

                // For standard properties we save only custom params (that start with 'X-'). All standard params go to their fields in DB.
                // For custom properies we save all params.
                if (!isCustomProp && !paramName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase))
                    continue;

                foreach (string value in param.Values)
                {
                    string val = value;

                    valuesSql.Add(string.Format(@"(
                                  @ParentId{0}
                                , @UID{0}
                                , @ClientAppName{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                , @SortIndexParam{0}
                                )", paramIndex));

                    parameters.AddRange(new object[] {
                                  "@ParentId"       + paramIndex, parentId
                                , "@UID"            + paramIndex, uid          // added for performance optimization purposes
                                , "@ClientAppName"  + paramIndex, clientAppName// Client app name that created this custom parameter.
                                , "@PropertyName"   + paramIndex, propName
                                , "@ParameterName"  + paramIndex, paramName
                                , "@Value"          + paramIndex, val
                                , "@SortIndexParam" + paramIndex, null         // Property position in vCard. Null is inserted for parameter values.
                                });
                    paramIndex++;
                }
            }

            if (origParamsCount < parameters.Count())
            {
                sql = string.Format(sql, string.Join(", ", valuesSql.ToArray()));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Creates SQL to write custom properties and parameters to [card_CustomProperty] table for specified component.
        /// </summary>
        /// <param name="component">Component to be saved to database.</param>
        /// <param name="parentId">
        /// Parent component ID to which this custom property or parameter belongs to. 
        /// This could be UID (in case of [card_CardFile] table), EmailId, InstantMessengerId, etc.
        /// </param>
        /// <param name="uid">Card UID.</param>
        /// <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        /// <param name="sql">SQL to insert data to DB.</param>
        /// <param name="parameters">SQL parameter values that will be filled by this method.</param>
        /// <returns>True if any custom properies or parameters found, false otherwise.</returns>
        private bool PrepareSqlCustomPropertiesOfComponentAsync(IComponent component, string parentId, string uid, string clientAppName, out string sql, out List<object> parameters)
        {
            sql = "";
            parameters = new List<object>();

            // We save only single custom props here, multiple props are saved in other methods.
            string[] multiProps = new string[] { "EMAIL", "ADR", "IMPP", "TEL", "URL" };

            // Properties in IComponent.Properties are grouped by name.
            foreach (KeyValuePair<string, IList<IRawProperty>> pair in component.Properties)
            {
                if (multiProps.Contains(pair.Key.ToUpper()) || (pair.Value.Count != 1))
                    continue;

                string sqlInsert;
                List<object> parametersInsert;
                if (PrepareSqlParamsWriteCustomProperty(pair.Key, pair.Value.First(), parentId, uid, clientAppName, out sqlInsert, out parametersInsert))
                {
                    sql += "; " + sqlInsert;
                    parameters.AddRange(parametersInsert);
                }
            }

            return !string.IsNullOrEmpty(sql);
        }

        /// <summary>
        /// Called when client application deletes this file.
        /// </summary>
        /// <param name="multistatus">Error description if case delate failed. Ignored by most clients.</param>
        public override async Task DeleteAsync(MultistatusException multistatus)
        {
            string sql = @"DELETE FROM [card_CardFile] 
                           WHERE FileName=@FileName
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Write] = 1)";

            if(await Context.ExecuteNonQueryAsync(sql, 
                  "@UserId"   , Context.UserId
                , "@FileName" , fileName) < 1)
            {
                throw new DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN);
            }
        }

        /// <summary>
        /// Called when a card must be read from back-end storage.
        /// </summary>
        /// <param name="output">Stream to write vCard content.</param>
        /// <param name="startIndex">Index to start reading data from back-end storage. Used for segmented reads, not used by CardDAV clients.</param>
        /// <param name="count">Number of bytes to read. Used for segmented reads, not used by CardDAV clients.</param>
        public async Task ReadAsync(Stream output, long startIndex, long count)
        {
            string vCardVersion = rowCardFile.Field<string>("Version");
            ICard2 card = CardFactory.CreateCard(vCardVersion);

            ReadCard(card);

            ReadEmails(card.Emails, rowsEmails);
            ReadAddresses(card.Addresses, rowsAddresses);
            ReadTelephones(card.Telephones, rowsTelephones);
            ReadUrls(card.Urls, rowsUrls);

            // IMPP is vCard 3.0 & 4.0 prop
            ICard3 card3 = card as ICard3;
            if (card3 != null)
            {
                ReadMessengers(card3.InstantMessengers, rowsInstantMessengers);
            }

            // Check if this CardDAV client application requires properties conversion.
            if (AppleCardInteroperability.NeedsConversion(Context.Request.UserAgent))
            {
                // In case of iOS & OS X the props below must be converted to the following format:
                // item2.TEL:(222)222 - 2222
                // item2.X-ABLabel:Emergency
                AppleCardInteroperability.Denormalize(card);
            }

            new vFormatter().Serialize(output, card);
        }

        /// <summary>
        /// Reads data from [card_CardFile] row.
        /// </summary>
        /// <param name="card">Card that will be populated from row paramater.</param>
        private void ReadCard(ICard2 card)
        {
            string uid = rowCardFile.Field<string>("UID");

            //UID
            card.Uid = card.CreateTextProp(uid);


            // PRODID
            card.ProductId = card.CreateTextProp("-//IT Hit//Collab Lib//EN");

            // FN
            card.FormattedNames.Add(rowCardFile.Field<string>("FormattedName"));

            // N
            card.Name = card.CreateNameProp(
                rowCardFile.Field<string>("FamilyName"),
                rowCardFile.Field<string>("GivenName"),
                rowCardFile.Field<string>("AdditionalNames"),
                rowCardFile.Field<string>("HonorificPrefix"),
                rowCardFile.Field<string>("HonorificSuffix"));

            // PHOTO
            if (!rowCardFile.IsNull("Photo"))
            {
                card.Photos.Add(Convert.ToBase64String(rowCardFile.Field<byte[]>("Photo")), rowCardFile.Field<string>("PhotoMediaType"), false);
            }

            // LOGO
            if (!rowCardFile.IsNull("Logo"))
            {
                card.Photos.Add(Convert.ToBase64String(rowCardFile.Field<byte[]>("Logo")), rowCardFile.Field<string>("LogoMediaType"), false);
            }

            // SOUND
            if (!rowCardFile.IsNull("Sound"))
            {
                card.Photos.Add(Convert.ToBase64String(rowCardFile.Field<byte[]>("Sound")), rowCardFile.Field<string>("SoundMediaType"), false);
            }

            // BDAY
            DateTime? birthday = rowCardFile.Field<DateTime?>("Birthday");
            if (birthday != null)
            {
                card.BirthDate = card.CreateDateProp(birthday.Value, DateComponents.Date);
            }

            // REV
            DateTime? revision = rowCardFile.Field<DateTime?>("RevisionUtc");
            if (revision != null)
            {
                card.Revision = card.CreateDateProp(revision.Value);
            }

            // SORT-STRING
            string sortString = rowCardFile.Field<string>("SortString");
            if (!string.IsNullOrEmpty(sortString))
            {
                ITextProperty2 propSortString = card.CreateProperty<ITextProperty2>();
                propSortString.Text = sortString;
                card.SortString = propSortString;
            }

            // TZ
            string timeZone = rowCardFile.Field<string>("TimeZone");
            if (!string.IsNullOrEmpty(timeZone))
            {
                card.TimeZones.Add(timeZone);
            }

            // GEO

            // TITLE
            string title = rowCardFile.Field<string>("Title");
            if (!string.IsNullOrEmpty(title))
            {
                card.Titles.Add(title);
            }

            // ROLE
            string role = rowCardFile.Field<string>("Role");
            if (!string.IsNullOrEmpty(role))
            {
                card.Roles.Add(role);
            }

            // ORG
            string orgName = rowCardFile.Field<string>("OrgName");
            string orgUnit = rowCardFile.Field<string>("OrgUnit");
            if (!string.IsNullOrEmpty(orgName) || !string.IsNullOrEmpty(orgUnit))
            {
                IOrganization2 propOrg = card.Organizations.CreateProperty();
                propOrg.Name = orgName;
                propOrg.Units = new[] { orgUnit };
                card.Organizations.Add(propOrg);
            }

            // NOTE
            string note = rowCardFile.Field<string>("Note");
            if (!string.IsNullOrEmpty(note))
            {
                card.Notes.Add(note);
            }

            // vCard v3.0 & v4.0 props
            if (card is ICard3)
            {
                ICard3 card3 = card as ICard3;

                // NICKNAME
                string nickname = rowCardFile.Field<string>("Nickname");
                if (!string.IsNullOrEmpty(nickname))
                {
                    INickname3 propNickname = card3.NickNames.CreateProperty();
                    propNickname.Values = new[] { nickname };
                    card3.NickNames.Add(propNickname);
                }

                // CATEGORIES
                string categories = rowCardFile.Field<string>("Categories");
                if (!string.IsNullOrEmpty(categories))
                {
                    string[] aCategories = categories.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string categoryList in aCategories)
                    {
                        ICategories3 catProp = card3.Categories.CreateProperty();
                        catProp.Values = categoryList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        card3.Categories.Add(catProp);
                    }
                }

                // CLASS
                string classification = rowCardFile.Field<string>("Classification");
                if (!string.IsNullOrEmpty(classification))
                {
                    card3.Classes.Add(classification);
                }
            }

            // vCard v4.0 props
            if (card is ICard4)
            {
                ICard4 card4 = card as ICard4;

                // KIND
                string kind = rowCardFile.Field<string>("Kind");
                if (kind != null)
                {
                    IKind4 propKind = card4.CreateProperty<IKind4>();
                    propKind.Text = kind;
                    card4.Kind = propKind;
                }

                // ANNIVERSARY
                DateTime? anniversary = rowCardFile.Field<DateTime?>("Anniversary");
                if (anniversary != null)
                {
                    IAnniversary4 propAnniversary = card4.CreateProperty<IAnniversary4>();
                    propAnniversary.Value = new Date(anniversary.Value, DateComponents.Month | DateComponents.Date);
                    card4.Anniversary = propAnniversary;
                }
                
                
                // GENDER
                string gender = rowCardFile.Field<string>("Gender");
                if (!string.IsNullOrEmpty(gender))
                {
                    IGender4 propGender = card4.CreateProperty<IGender4>();
                    propGender.Text = gender;
                    card4.Gender = propGender;
                }
                
                // LANG
                string language = rowCardFile.Field<string>("Language");
                if (!string.IsNullOrEmpty(language))
                {
                    card4.ContactLanguages.Add(language);
                }
            }


            // Get custom properties and custom parameters
            IEnumerable<DataRow> rowsCardCustomProperties = rowsCustomProperties.Where(x => x.Field<string>("ParentId") == uid);
            ReadCustomProperties(card, rowsCardCustomProperties);
        }


        /// <summary>
        /// Reads data from [card_Email] rows.
        /// </summary>
        /// <param name="emails">Empty emails list that will be populated with data from <paramref name="rowsEmails"/> parameter.</param>
        /// <param name="rowsEmails">Data from [card_Email] table to populate <paramref name="emails"/> parameter.</param>
        private void ReadEmails(ITextPropertyList<IEmail2> emails, IEnumerable<DataRow> rowsEmails)
        {
            foreach (DataRow rowEmail in rowsEmails)
            {
                IEmail2 email = emails.CreateProperty();
                email.Text  = rowEmail.Field<string>("Email");                              // EMAIL value
                email.Types = ParseType<EmailType>(rowEmail.Field<string>("Type"));         // TYPE param
                SetPrefParameter(email, rowEmail.Field<byte?>("PreferenceLevel"));          // PREF param
                email.RawProperty.SortIndex = rowEmail.Field<int>("SortIndex");             // Property position in vCard.
                AddParamValues(rowEmail.Field<Guid>("EmailId"), email.RawProperty);         // Add custom parameters from [card_CustomProperty] table.
                emails.Add(email);
            }
        }

        /// <summary>
        /// Reads data from [card_Address] rows.
        /// </summary>
        /// <param name="addresses">Empty addresses list that will be populated with data from <paramref name="rowsAddresses"/> parameter.</param>
        /// <param name="rowsAddresses">Data from [card_Address] table to populate <paramref name="addresses"/> parameter.</param>
        private void ReadAddresses(ICardPropertyList<IAddress2> addresses, IEnumerable<DataRow> rowsAddresses)
        {
            foreach (DataRow rowAddress in rowsAddresses)
            {
                IAddress2 address = addresses.CreateProperty();
                address.SetAddress(
                    new[] { rowAddress.Field<string>("PoBox") },
                    new[] { rowAddress.Field<string>("AppartmentNumber")},
                    new[] { rowAddress.Field<string>("Street")},
                    new[] { rowAddress.Field<string>("Locality")},
                    new[] { rowAddress.Field<string>("Region")},
                    new[] { rowAddress.Field<string>("PostalCode")},
                    new[] { rowAddress.Field<string>("Country")},
                    ParseType<AddressType>(rowAddress.Field<string>("Type")));              // ADR value and TYPE param                
                SetPrefParameter(address, rowAddress.Field<byte?>("PreferenceLevel"));      // PREF param
                address.RawProperty.SortIndex = rowAddress.Field<int>("SortIndex");         // Property position in vCard.
                AddParamValues(rowAddress.Field<Guid>("AddressId"), address.RawProperty);   // Add custom parameters from [card_CustomProperty] table.
                addresses.Add(address);
            }
        }

        /// <summary>
        /// Reads data from [card_InstantMessenger] rows.
        /// </summary>
        /// <param name="messengers">Empty emails list that will be populated with data from <paramref name="rowsMessengers"/> parameter.</param>
        /// <param name="rowsMessengers">Data from [card_InstantMessenger] table to populate <paramref name="messengers"/> parameter.</param>
        private void ReadMessengers(ITextPropertyList<IInstantMessenger3> messengers, IEnumerable<DataRow> rowsMessengers)
        {
            foreach (DataRow rowMessenger in rowsMessengers)
            {
                IInstantMessenger3 messenger = messengers.CreateProperty();
                messenger.Text  = rowMessenger.Field<string>("InstantMessenger");                       // IMPP value
                messenger.Types = ParseType<MessengerType>(rowMessenger.Field<string>("Type"));         // TYPE param
                SetPrefParameter(messenger, rowMessenger.Field<byte?>("PreferenceLevel"));              // PREF param
                messenger.RawProperty.SortIndex = rowMessenger.Field<int>("SortIndex");                 // Property position in vCard.
                AddParamValues(rowMessenger.Field<Guid>("InstantMessengerId"), messenger.RawProperty);  // Add custom parameters from [card_CustomProperty] table.
                messengers.Add(messenger);
            }
        }

        /// <summary>
        /// Reads data from [card_Telephone] rows.
        /// </summary>
        /// <param name="telephones">Empty emails list that will be populated with data from <paramref name="rowsTelephones"/> parameter.</param>
        /// <param name="rowsTelephones">Data from [card_Telephone] table to populate <paramref name="telephones"/> parameter.</param>
        private void ReadTelephones(ITextPropertyList<ITelephone2> telephones, IEnumerable<DataRow> rowsTelephones)
        {
            foreach (DataRow rowTelephone in rowsTelephones)
            {
                ITelephone2 telephone = telephones.CreateProperty();
                telephone.Text  = rowTelephone.Field<string>("Telephone");                      // TEL value
                telephone.Types = ParseType<TelephoneType>(rowTelephone.Field<string>("Type")); // TYPE param
                SetPrefParameter(telephone, rowTelephone.Field<byte?>("PreferenceLevel"));      // PREF param
                telephone.RawProperty.SortIndex = rowTelephone.Field<int>("SortIndex");         // Property position in vCard.
                AddParamValues(rowTelephone.Field<Guid>("TelephoneId"), telephone.RawProperty); // Add custom parameters from [card_CustomProperty] table.
                telephones.Add(telephone);
            }
        }

        /// <summary>
        /// Reads data from [card_Url] rows.
        /// </summary>
        /// <param name="urls">Empty URLs list that will be populated with data from <paramref name="rowsUrls"/> parameter.</param>
        /// <param name="rowsUrls">Data from [card_Url] table to populate <paramref name="urls"/> parameter.</param>
        private void ReadUrls(ICardPropertyList<ICardUriProperty2> urls, IEnumerable<DataRow> rowsUrls)
        {
            foreach (DataRow rowUrl in rowsUrls)
            {
                ICardUriProperty2 url = urls.CreateProperty();
                url.Text  = rowUrl.Field<string>("Url");                                    // URL value
                url.Types = ParseType<ExtendibleEnum>(rowUrl.Field<string>("Type"));        // TYPE param
                SetPrefParameter(url, rowUrl.Field<byte?>("PreferenceLevel"));              // PREF param
                url.RawProperty.SortIndex = rowUrl.Field<int>("SortIndex");                 // Property position in vCard.
                AddParamValues(rowUrl.Field<Guid>("UrlId"), url.RawProperty);               // Add custom parameters from [card_CustomProperty] table.
                urls.Add(url);
            }
        }

        /// <summary>
        /// Sets PREF parameter.
        /// </summary>
        /// <param name="prop">Property to set the PREF parameter.</param>
        /// <param name="preferenceLevel">Preference level. If null is passed PREF is not set.</param>
        private static void SetPrefParameter(ICardMultiProperty prop, byte? preferenceLevel)
        {
            if (preferenceLevel != null)
            {
                ICardMultiProperty4 prop4 = prop as ICardMultiProperty4;
                if (prop4 == null)
                {
                    // vCard 2.1 & 3.0
                    prop.IsPrefered = true;
                }
                else
                {
                    // vCard 4.0
                    prop4.PreferenceLevel = preferenceLevel.Value;
                }
            }
        }

        /// <summary>
        /// Parses TYPE parameter.
        /// </summary>
        /// <param name="typesList">Coma separated list of types.</param>
        private static T[] ParseType<T>(string typesList) where T : ExtendibleEnum, new()
        {
            if (!string.IsNullOrEmpty(typesList))
            {
                string[] aStrTypes = typesList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                return aStrTypes.Select(x => StringToEnum<T>(x)).ToArray();
            }

            return new T[] { };
        }

        /// <summary>
        /// Reads custom properties and parameters from [card_CustomProperty] table
        /// and creates them in component passed as a parameter.
        /// </summary>
        /// <param name="component">Component where custom properties and parameters will be created.</param>
        /// <param name="rowsCustomProperies">Custom properties datat from [card_CustomProperty] table.</param>
        private static void ReadCustomProperties(IComponent component, IEnumerable<DataRow> rowsCustomProperies)
        {
            foreach (DataRow rowCustomProperty in rowsCustomProperies)
            {
                string propertyName = rowCustomProperty.Field<string>("PropertyName");

                IRawProperty prop;
                if (!component.Properties.ContainsKey(propertyName))
                {
                    prop = component.CreateRawProperty();
                    component.AddProperty(propertyName, prop);
                }
                else
                {
                    prop = component.Properties[propertyName].FirstOrDefault();
                }

                string paramName = rowCustomProperty.Field<string>("ParameterName");
                string value = rowCustomProperty.Field<string>("Value");
                if (paramName == null)
                {
                    // If ParameterName is null the Value contains property value
                    prop.RawValue = value;
                    prop.SortIndex = rowCustomProperty.Field<int>("SortIndex"); // Property position in vCard.
                }
                else
                {
                    prop.Parameters.Add(new Parameter(paramName, value));
                }
            }
        }

        /// <summary>
        /// Adds custom parameters to property.
        /// </summary>
        /// <param name="propertyId">ID from [card_CardFile], [card_Email], [card_Address], [card_Telephone], [card_InstantMessenger] tables. Used to find parameters in [CardCustomProperties] table.</param>
        /// <param name="prop">Property to add parameters to.</param>
        private void AddParamValues(Guid propertyId, IRawProperty prop)
        {
            IEnumerable<DataRow> rowsCustomParams = rowsCustomProperties.Where(x => x.Field<string>("ParentId") == propertyId.ToString());
            foreach (DataRow rowCustomParam in rowsCustomParams)
            {
                string paramName = rowCustomParam.Field<string>("ParameterName");
                string paramValue = rowCustomParam.Field<string>("Value");
                prop.Parameters.Add(new Parameter(paramName, paramValue));
            }
        }

        /// <summary>
        /// Converts string to <see cref="ExtendibleEnum"/> of spcified type. Returns <b>null</b> if <b>null</b> is passed. 
        /// If no matching string value is found the <see cref="ExtendibleEnum.Name"/> is set to passed parameter <b>value</b> and <see cref="ExtendibleEnum.Number"/> is set to -1.
        /// </summary>
        /// <typeparam name="T">Type to convert to.</typeparam>
        /// <param name="value">String to convert from.</param>
        /// <returns><see cref="ExtendibleEnum"/> of type <b>T</b> or <b>null</b> if <b>null</b> is passed as a parameter.</returns>
        private static T StringToEnum<T>(string value) where T : ExtendibleEnum, new()
        {
            if (value == null)
                return null;

            T res;
            if (!ExtendibleEnum.TryFromString<T>(value, out res))
            {
                // If no matching value is found create new ExtendibleEnum or type T 
                // with specified string value and default numeric value (-1).
                res = new T();
                res.Name = value;
            }

            return res;
        }
    }
}
