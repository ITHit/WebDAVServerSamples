Imports System
Imports System.Linq
Imports System.IO
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Data
Imports System.Data.SqlClient
Imports System.Diagnostics
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.Collab
Imports ITHit.Collab.Card
Imports ITHit.Server

Namespace CardDav

    ''' <summary>
    ''' Represents a vCard file.
    ''' Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[AddressbookFolderId]/[FileName].vcf
    ''' </summary>
    Public Class CardFile
        Inherits DavHierarchyItem
        Implements ICardFileAsync

        ''' <summary>
        ''' Card file extension.
        ''' </summary>
        Public Shared Extension As String = ".vcf"

        ''' <summary>
        ''' Loads card files contained in an addressbook folder by address book folder ID.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="addressbookFolderId">Address book for which cards should be loaded.</param>
        ''' <param name="propsToLoad">Specifies which properties should be loaded.</param>
        ''' <returns>List of <see cref="ICardFileAsync"/>  items.</returns>
        Public Shared Async Function LoadByAddressbookFolderIdAsync(context As DavContext, addressbookFolderId As Guid, propsToLoad As PropsToLoad) As Task(Of IEnumerable(Of ICardFileAsync))
            ' propsToLoad == PropsToLoad.Minimum -> Typical GetChildren call by iOS, Android, eM Client, etc CardDAV clients
            ' [Summary] is typically not required in GetChildren call, 
            ' they are extracted for demo purposes only, to be displayed in Ajax File Browser.
            ' propsToLoad == PropsToLoad.All -> Bynari call, it requires all props in GetChildren call.
            If propsToLoad <> PropsToLoad.Minimum Then Throw New NotImplementedException("LoadByAddressbookFolderIdAsync is implemented only with PropsToLoad.Minimum.")
            Dim sql As String = "SELECT * FROM [card_CardFile] 
                           WHERE [AddressbookFolderId] = @AddressbookFolderId
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)"
            'sql = string.Format(sql, GetScPropsToLoad(propsToLoad));
            Return Await LoadAsync(context, sql,
                                  "@UserId", context.UserId,
                                  "@AddressbookFolderId", addressbookFolderId)
        End Function

        ''' <summary>
        ''' Loads card files by list of their names.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="fileNames">File names to load.</param>
        ''' <param name="propsToLoad">Specifies which properties should be loaded.</param>
        ''' <returns>List of <see cref="ICardFileAsync"/>  items.</returns>
        Public Shared Async Function LoadByFileNamesAsync(context As DavContext, fileNames As IEnumerable(Of String), propsToLoad As PropsToLoad) As Task(Of IEnumerable(Of ICardFileAsync))
            ' Get IN clause part with list of file UIDs for SELECT.
            Dim selectIn As String = String.Join(", ", fileNames.Select(Function(a) String.Format("'{0}'", a)).ToArray())
            Dim sql As String = "SELECT * FROM [card_CardFile] 
                           WHERE [FileName] IN ({0})
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId)"
            If propsToLoad = PropsToLoad.All Then
                sql += ";SELECT * FROM [card_Email]             WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_Address]           WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_InstantMessenger]  WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_Telephone]         WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_Url]               WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0}))
                         ;SELECT * FROM [card_CustomProperty]    WHERE [UID] IN (SELECT UID FROM [card_CardFile] WHERE [FileName] IN ({0})) AND [ClientAppName] = @ClientAppName"
            End If

            sql = String.Format(sql, selectIn)
            Return Await LoadAsync(context, sql,
                                  "@UserId", context.UserId,
                                  "@ClientAppName", AppleCardInteroperability.GetClientAppName(context.Request.UserAgent))
        End Function

        ''' <summary>
        ''' Loads card files by SQL.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="sql">SQL that queries [card_CardFile], [card_Email], etc tables.</param>
        ''' <param name="prms">List of SQL parameters.</param>
        ''' <returns>List of <see cref="ICardFileAsync"/>  items.</returns>
        Private Shared Async Function LoadAsync(context As DavContext, sql As String, ParamArray prms As Object()) As Task(Of IEnumerable(Of ICardFileAsync))
            Dim items As IList(Of ICardFileAsync) = New List(Of ICardFileAsync)()
            Dim stopWatch As Stopwatch = Stopwatch.StartNew()
            Using reader As SqlDataReader = Await context.ExecuteReaderAsync(sql, prms)
                Dim cards As DataTable = New DataTable()
                cards.Load(reader)
                Dim emails As DataTable = New DataTable()
                If Not reader.IsClosed Then emails.Load(reader)
                Dim addresses As DataTable = New DataTable()
                If Not reader.IsClosed Then addresses.Load(reader)
                Dim instantMessengers As DataTable = New DataTable()
                If Not reader.IsClosed Then instantMessengers.Load(reader)
                Dim telephones As DataTable = New DataTable()
                If Not reader.IsClosed Then telephones.Load(reader)
                Dim urls As DataTable = New DataTable()
                If Not reader.IsClosed Then urls.Load(reader)
                Dim cardCustomProperties As DataTable = New DataTable()
                If Not reader.IsClosed Then cardCustomProperties.Load(reader)
                stopWatch.Stop()
                context.Engine.Logger.LogDebug(String.Format("SQL took: {0}ms", stopWatch.ElapsedMilliseconds))
                For Each rowCardFile As DataRow In cards.Rows
                    Dim rowsEmails As DataRow() = New DataRow(-1) {}
                    Dim rowsAddresses As DataRow() = New DataRow(-1) {}
                    Dim rowsInstantMessengers As DataRow() = New DataRow(-1) {}
                    Dim rowsTelephones As DataRow() = New DataRow(-1) {}
                    Dim rowsUrls As DataRow() = New DataRow(-1) {}
                    Dim rowsCustomProperties As DataRow() = New DataRow(-1) {}
                    Dim uid As String = rowCardFile.Field(Of String)("UID")
                    If emails.Columns("UID") IsNot Nothing Then
                        Dim filter As String = String.Format("UID = '{0}'", uid)
                        rowsEmails = emails.Select(filter)
                        rowsAddresses = addresses.Select(filter)
                        rowsInstantMessengers = instantMessengers.Select(filter)
                        rowsTelephones = telephones.Select(filter)
                        rowsUrls = urls.Select(filter)
                        rowsCustomProperties = cardCustomProperties.Select(filter)
                    End If

                    Dim fileName As String = rowCardFile.Field(Of String)("FileName")
                    items.Add(New CardFile(context, fileName, rowCardFile, rowsEmails, rowsAddresses, rowsInstantMessengers, rowsTelephones, rowsUrls, rowsCustomProperties))
                Next
            End Using

            Return items
        End Function

        ''' <summary>
        ''' Creates a new card file. The actual new [card_CardFile], [card_Email], etc. records are inserted into the database during <see cref="WriteAsync"/>  method call.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param> 
        ''' <param name="addressbookFolderId">Address book folder ID to which this card file will belong to.</param>
        ''' <param name="fileName">New card file name.</param>
        ''' <returns>Instance of <see cref="CardFile"/> .</returns>
        Public Shared Function CreateCardFile(context As DavContext, addressbookFolderId As Guid, fileName As String) As CardFile
            Dim cardFile As CardFile = New CardFile(context, fileName, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing)
            cardFile.addressbookFolderId = addressbookFolderId
            Return cardFile
        End Function

        ''' <summary>
        ''' This card file name.
        ''' </summary>
        Private ReadOnly fileName As String = Nothing

        ''' <summary>
        ''' Contains data from [card_CardFile] table.
        ''' </summary>
        Private ReadOnly rowCardFile As DataRow = Nothing

        ''' <summary>
        ''' Contains e-mails for this card from [card_Email] table.
        ''' </summary>
        Private ReadOnly rowsEmails As DataRow() = Nothing

        ''' <summary>
        ''' Contains addresses for this card from [card_Address] table.
        ''' </summary>
        Private ReadOnly rowsAddresses As DataRow() = Nothing

        ''' <summary>
        ''' Contains instant messengers for this card from [card_InstantMessenger] table.
        ''' </summary>
        Private ReadOnly rowsInstantMessengers As DataRow() = Nothing

        ''' <summary>
        ''' Contains telephones for this card from [card_Telephone] table.
        ''' </summary>
        Private ReadOnly rowsTelephones As DataRow() = Nothing

        ''' <summary>
        ''' Contains URLs for this card from [card_Url] table.
        ''' </summary>
        Private ReadOnly rowsUrls As DataRow() = Nothing

        ''' <summary>
        ''' Contains custom properties and custom parameters for this card, it's 
        ''' e-mails, addresses, instant messengers or telephones form [card_CustomProperty] table.
        ''' </summary>
        Private ReadOnly rowsCustomProperties As DataRow() = Nothing

        ''' <summary>
        ''' Indicates if this is a newly created card.
        ''' </summary>
        Private ReadOnly Property isNew As Boolean
            Get
                Return addressbookFolderId <> Guid.Empty
            End Get
        End Property

        ''' <summary>
        ''' Used to form unique SQL parameter names.
        ''' </summary>
        Private paramIndex As Integer = 0

        ''' <summary>
        ''' Addressbook folder ID in which the new card will be created.
        ''' </summary>
        Private addressbookFolderId As Guid = Guid.Empty

        ''' <summary>
        ''' Gets display name of the card. Used for demo purposes only, to be displayed in Ajax File Browser.
        ''' </summary>
        ''' <remarks>CardDAV clients typically never request this property.</remarks>
        Public Overrides ReadOnly Property Name As String Implements IHierarchyItemBaseAsync.Name
            Get
                Return rowCardFile.Field(Of String)("FormattedName")
            End Get
        End Property

        ''' <summary>
        ''' Gets item path.
        ''' </summary>
        ''' <remarks>[DAVLocation]/addressbooks/[AddressbookFolderId]/[FileName].vcf</remarks>
        Public Overrides ReadOnly Property Path As String Implements IHierarchyItemBaseAsync.Path
            Get
                Dim addressbookFolderId As Guid = rowCardFile.Field(Of Guid)("AddressbookFolderId")
                Dim fileName As String = rowCardFile.Field(Of String)("FileName")
                Return String.Format("{0}{1}/{2}{3}", AddressbooksRootFolder.AddressbooksRootFolderPath, addressbookFolderId, fileName, Extension)
            End Get
        End Property

        ''' <summary>
        ''' Gets eTag. ETag must change every time this card is updated.
        ''' </summary>
        Public ReadOnly Property Etag As String Implements IContentAsync.Etag
            Get
                Dim bETag As Byte() = rowCardFile.Field(Of Byte())("ETag")
                Return BitConverter.ToUInt64(bETag.Reverse().ToArray(), 0).ToString()
            End Get
        End Property

        ''' <summary>
        ''' Gets item creation date. Must be in UTC.
        ''' </summary>
        Public Overrides ReadOnly Property Created As DateTime Implements IHierarchyItemBaseAsync.Created
            Get
                Return rowCardFile.Field(Of DateTime)("CreatedUtc")
            End Get
        End Property

        ''' <summary>
        ''' Gets item modification date. Must be in UTC.
        ''' </summary>
        Public Overrides ReadOnly Property Modified As DateTime Implements IHierarchyItemBaseAsync.Modified
            Get
                Return rowCardFile.Field(Of DateTime)("ModifiedUtc")
            End Get
        End Property

        ''' <summary>
        ''' File content length. Typicaly never requested by CardDAV clients.
        ''' </summary>
        ''' <remarks>
        ''' If -1 is returned the chunked response will be generated if possible. The getcontentlength property will not be generated.
        ''' </remarks>
        Public ReadOnly Property ContentLength As Long Implements IContentAsync.ContentLength
            Get
                Return -1
            End Get
        End Property

        ''' <summary>
        ''' File Mime-type/Content-Type.
        ''' </summary>
        Public ReadOnly Property ContentType As String Implements IContentAsync.ContentType
            Get
                Return "text/vcard"
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CardFile"/>  class from database source. 
        ''' Used when listing folder content and during multi-get requests.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="fileName">Card file name. Unlike with CalDAV, in case of CardDAV this is different from UID.</param>
        ''' <param name="rowCardFile">Card file info from [card_CardFile] table.</param>
        ''' <param name="rowsEmails">List of e-mails for this card from [card_Email] table.</param>
        ''' <param name="rowsAddresses">List of addresses for this card from [card_Address] table.</param>
        ''' <param name="rowsInstantMessengers">List of instant messengers for this card from [card_InstantMessenger] table.</param>
        ''' <param name="rowsTelephones">List of telephones for this card from [card_Telephone] table.</param>
        ''' <param name="rowsUrls">List of URLs for this card from [card_Url] table.</param>
        ''' <param name="rowsCustomProperties">List of vCard custom properties and parameters for this card from [card_CustomProperty] table.</param>
        Private Sub New(context As DavContext, fileName As String,
                       rowCardFile As DataRow, rowsEmails As DataRow(), rowsAddresses As DataRow(), rowsInstantMessengers As DataRow(),
                       rowsTelephones As DataRow(), rowsUrls As DataRow(), rowsCustomProperties As DataRow())
            MyBase.New(context)
            Me.fileName = fileName
            Me.rowCardFile = rowCardFile
            Me.rowsEmails = rowsEmails
            Me.rowsAddresses = rowsAddresses
            Me.rowsInstantMessengers = rowsInstantMessengers
            Me.rowsTelephones = rowsTelephones
            Me.rowsUrls = rowsUrls
            Me.rowsCustomProperties = rowsCustomProperties
        End Sub

        ''' <summary>
        ''' Called when card is being saved to back-end storage.
        ''' </summary>
        ''' <param name="stream">Stream containing VCARD.</param>
        ''' <param name="contentType">Content type.</param>
        ''' <param name="startIndex">Starting byte in target file
        ''' for which data comes in <paramref name="content"/>  stream.</param>
        ''' <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        ''' <returns>Whether the whole stream has been written.</returns>
        Public Async Function WriteAsync(stream As Stream, contentType As String, startIndex As Long, totalFileSize As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
            'Set timeout to maximum value to be able to upload large card files.
            System.Web.HttpContext.Current.Server.ScriptTimeout = Integer.MaxValue
            Dim vCard As String
            Using reader As StreamReader = New StreamReader(stream)
                vCard = reader.ReadToEnd()
            End Using

            ' Typically the stream contains a single vCard.
            Dim cards As IEnumerable(Of IComponent) = New vFormatter().Deserialize(vCard)
            Dim card As ICard2 = TryCast(cards.First(), ICard2)
            ' Card file UID which is equal to file name.
            Dim uid As String = card.Uid.Text
            ' Check if this CardDAV client application requires properties conversion.
            If AppleCardInteroperability.NeedsConversion(Context.Request.UserAgent) Then
                ''' Replace "itemX.PROP" properties created by iOS and OS X with "PROP", so they 
                ''' are saved as normal props to storage and can be read by any CardDAV client.
                AppleCardInteroperability.Normalize(card)
            End If

            ' The client app name is stored in DB to update and extract only custom props created by the client making a request.
            Dim clientAppName As String = AppleCardInteroperability.GetClientAppName(Context.Request.UserAgent)
            ' Save data to [card_CardFile] table.
            Await WriteCardFileAsync(Context, card, addressbookFolderId, isNew, clientAppName)
            ' Save emails.
            Await WriteEmailsAsync(Context, card.Emails, uid, clientAppName)
            ' Save addresses.
            Await WriteAddressesAsync(Context, card.Addresses, uid, clientAppName)
            ' Save telephones.
            Await WriteTelephonesAsync(Context, card.Telephones, uid, clientAppName)
            ' Save URLs
            Await WriteUrlsAsync(Context, card.Urls, uid, clientAppName)
            ' Save instant messengers. vCard 3.0+ only
            Dim card3 As ICard3 = TryCast(card, ICard3)
            If card3 IsNot Nothing Then
                Await WriteInstantMessengersAsync(Context, card3.InstantMessengers, uid, clientAppName)
            End If

            Return True
        End Function

        ''' <summary>
        ''' Saves data to [card_CardFile] table.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="card">Card to read data from.</param>
        ''' <param name="addressbookFolderId">Address book folder that contains this file.</param>
        ''' <param name="isNew">Flag indicating if this is a new file or file should be updated.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        ''' <remarks>
        ''' This function deletes records in [card_Email], [card_Address], [card_InstantMessenger],
        ''' [card_Telephone], [card_Url] tables if the card should be updated. Values from the [card_CustomProperty] table 
        ''' is being deleted if updated by the same client that created a specific custom property.
        ''' </remarks>
        Private Async Function WriteCardFileAsync(context As DavContext, card As ICard2, addressbookFolderId As Guid, isNew As Boolean, clientAppName As String) As Task
            Dim sql As String
            If isNew Then
                sql = "IF EXISTS (SELECT 1 FROM [card_Access] WHERE [AddressbookFolderId]=@AddressbookFolderId AND [UserId]=@UserId AND [Write]=1)
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
                    )"
            Else
                ' We can only update record in [card_CardFile] table.
                ' There is no way to update [card_Email], [card_Address], [card_InstantMessenger], [card_Telephone] and [card_Url]
                ' tables for existing card, we must delete all records for this UID and recreate.
                ' We can keep custom props in [card_CustomProperty] table if they were created by a different CardDAV client.
                ' [ModifiedUtc] field update triggers [ETag] field update which is used for synchronyzation.
                sql = "IF EXISTS (SELECT 1 FROM [card_CardFile] 
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
                      END"
            End If

            ' [ClientAppName] = @ClientAppName -> delete all custom props created by this client.
            ' [ParentId] != [UID]              -> delete all custom params from multiple props: EMAIL, ADR, TEL, IMPP. Keep custom params for any single props in [card_Card].
            ' [ClientAppName] IS NULL          -> delete all custom props created by some unknown CardDAV client.
            Dim uid As String = card.Uid.Text
            If Await context.ExecuteNonQueryAsync(sql,
                                                 "@UID", uid,                                                                   ' UID
                                                 "UserId", context.UserId,
                                                 "@AddressbookFolderId", addressbookFolderId,                                               ' used only when inserting
                                                 "@FileName", fileName,                                                              ' In case of CardDAV a file name is sypically a GUID, but it is different from UID.
                                                 "@ModifiedUtc", DateTime.UtcNow,
                                                 "@Version", card.Version.Text,                                                     ' VERSION
                                                 "@Product", card.ProductId?.Text,                                                  ' PRODID
                                                 "@FormattedName", card.FormattedNames.PreferedOrFirstProperty.Text,                      ' FN                           Here we assume only 1 prop for the sake of simplicity.
                                                 "@FamilyName", card.Name.FamilyName,                                                  ' N
                                                 "@GivenName", card.Name.GivenName,                                                   ' N
                                                 "@AdditionalNames", card.Name.AdditionalNamesList,                                         ' N
                                                 "@HonorificPrefix", card.Name.HonorificPrefix,                                             ' N
                                                 "@HonorificSuffix", card.Name.HonorificSuffix,                                             ' N
                                                 "@Kind", TryCast(card, ICard4)?.Kind?.Text,                                          ' KIND         (vCard 4.0)
                                                 "@Nickname", TryCast(card, ICard3)?.NickNames.PreferedOrFirstProperty?.Values.First(),   ' NICKNAME     (vCard 3.0+)    Here we assume only 1 prop with 1 value for the sake of simplicity.
                                                 CreateVarBinaryParam("@Photo", card.Photos.PreferedOrFirstProperty?.Base64Data),           ' PHOTO                        Here we assume only 1 prop for the sake of simplicity.
                                                 "@PhotoMediaType", card.Photos.PreferedOrFirstProperty?.MediaType,                        ' PHOTO TYPE param
                                                 CreateVarBinaryParam("@Logo", card.Logos.PreferedOrFirstProperty?.Base64Data),            ' LOGO                         Here we assume only 1 prop for the sake of simplicity.
                                                 "@LogoMediaType", card.Logos.PreferedOrFirstProperty?.MediaType,                         ' LOGO  TYPE param
                                                 CreateVarBinaryParam("@Sound", card.Sounds.PreferedOrFirstProperty?.Base64Data),           ' SOUND                        Here we assume only 1 prop for the sake of simplicity.
                                                 "@SoundMediaType", card.Sounds.PreferedOrFirstProperty?.MediaType,                        ' SOUND TYPE param
                                                 New SqlParameter("@Birthday", If(card.BirthDate?.Value?.DateVal, TryCast(DBNull.Value, Object))) With {.SqlDbType = SqlDbType.DateTime2}, New SqlParameter("@Anniversary", If(TryCast(card, ICard4)?.Anniversary?.Value?.DateVal, TryCast(DBNull.Value, Object))) With {.SqlDbType = SqlDbType.DateTime2}, "@Gender", TryCast(card, ICard4)?.Gender?.Sex,                                         ' GENDER       (vCard 4.0)
                                                 "@RevisionUtc", card.Revision?.Value.DateVal,                                          ' REV
                                                 "@SortString", card.SortString?.Text,                                                 ' SORT-STRING
                                                 "@Language", TryCast(card, ICard4)?.ContactLanguages.PreferedOrFirstProperty.Text,       ' LANG         (vCard 4.0)     Here we assume only 1 prop for the sake of simplicity.
                                                 "@TimeZone", card.TimeZones.PreferedOrFirstProperty?.Text,                          ' TZ
                                                 "@Geo", Nothing, "@Title", card.Titles.PreferedOrFirstProperty?.Text,                             ' TITLE
                                                 "@Role", card.Roles.PreferedOrFirstProperty?.Text,                              ' ROLE
                                                 "@OrgName", card.Organizations.PreferedOrFirstProperty?.Name,                      ' ORG                          Here we assume only 1 prop for the sake of simplicity.
                                                 "@OrgUnit", card.Organizations.PreferedOrFirstProperty?.Units?.First(),            ' ORG                          Here we assume only 1 prop with 1 unit value for the sake of simplicity.
                                                 "@Categories", ListToString(Of String)(TryCast(card, ICard3)?.Categories.Select(Function(x) ListToString(Of String)(x.Values, ",")), ";"), ' CATEGORIES  (vCard 3.0+)
                                                 "@Note", card.Notes.PreferedOrFirstProperty?.Text,                              ' NOTE                         Here we assume only 1 prop for the sake of simplicity.
                                                 "@Classification", TryCast(card, ICard3)?.Classes.PreferedOrFirstProperty?.Text,               ' CLASS                        Here we assume only 1 prop for the sake of simplicity.
                                                 "@ClientAppName", clientAppName                                                         ' Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.
                                                 ) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If

            ' Save custom properties and parameters of this component to [card_CustomProperty] table.
            Dim customPropsSqlInsert As String
            Dim customPropsParamsInsert As List(Of Object)
            If PrepareSqlCustomPropertiesOfComponentAsync(card, uid, uid, clientAppName, customPropsSqlInsert, customPropsParamsInsert) Then
                Await context.ExecuteNonQueryAsync(customPropsSqlInsert, customPropsParamsInsert.ToArray())
            End If
        End Function

        ''' <summary>
        ''' Converts <see cref="IEnumerable{T}"/>  to string. Returns null if the list is empty.
        ''' </summary>
        ''' <returns>String that contains elements separated by ','.</returns>
        Private Shared Function ListToString(Of T)(arr As IEnumerable(Of T), Optional separator As String = ",") As String
            If(arr Is Nothing) OrElse Not arr.Any() Then Return Nothing
            Return String.Join(Of T)(separator, arr)
        End Function

        ''' <summary>
        ''' Creates a new SqlParameter with VARBINARY type and base64 content.
        ''' </summary>
        ''' <param name="parameterName">SQL parameter name.</param>
        ''' <param name="base64">Base 64-encoded parameter value.</param>
        ''' <returns></returns>
        Private Shared Function CreateVarBinaryParam(parameterName As String, base64 As String) As SqlParameter
            Dim param As SqlParameter = New SqlParameter(parameterName, SqlDbType.VarBinary)
            If String.IsNullOrEmpty(base64) Then
                ' To insert NULL to VARBINARY column, SqlParameter must be passed with Size=-1 and Value=DBNull.Value.
                param.Size = -1
                param.Value = DBNull.Value
            Else
                Dim content As Byte() = Convert.FromBase64String(base64)
                param.Size = content.Length
                param.Value = content
            End If

            Return param
        End Function

        ''' <summary>
        ''' Saves data to [card_Email] table.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="emails">List of emails to be saved.</param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        Private Async Function WriteEmailsAsync(context As DavContext, emails As ITextPropertyList(Of IEmail2), uid As String, clientAppName As String) As Task
            Dim sql As String = "INSERT INTO [card_Email] (
                      [EmailId]
                    , [UID]
                    , [Type]
                    , [Email]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each email As IEmail2 In emails
                valuesSql.Add(String.Format("(
                      @EmailId{0}
                    , @UID
                    , @Type{0}
                    , @Email{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i))
                Dim emailId As Guid = Guid.NewGuid()
                parameters.AddRange(New Object() {"@EmailId" & i, emailId,
                                                 "@Type" & i, ListToString(Of EmailType)(email.Types),    ' EMAIL TYPE param
                                                 "@Email" & i, email.Text,                              ' EMAIL VALUE
                                                 "@PreferenceLevel" & i, GetPrefParameter(email),                 ' EMAIL PREF param
                                                 "@SortIndex" & i, email.RawProperty.SortIndex             ' Property position in vCard.
                                                 })
                ' Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("EMAIL", email.RawProperty, emailId.ToString(), uid, clientAppName, customPropSqlInsert, customPropParametersInsert) Then
                    sql += "; " & customPropSqlInsert
                    parameters.AddRange(customPropParametersInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        ''' <summary>
        ''' Saves data to [card_Address] table.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="addresses">List of addresses to be saved.</param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        Private Async Function WriteAddressesAsync(context As DavContext, addresses As ICardPropertyList(Of IAddress2), uid As String, clientAppName As String) As Task
            Dim sql As String = "INSERT INTO [card_Address] (
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
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each address As IAddress2 In addresses
                valuesSql.Add(String.Format("(
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
                )", i))
                Dim addressId As Guid = Guid.NewGuid()
                parameters.AddRange(New Object() {"@AddressId" & i, addressId,
                                                 "@Type" & i, ListToString(Of AddressType)(address.Types),    ' ADR TYPE param
                                                 "@PoBox" & i, address.PoBox.FirstOrDefault(),
                                                 "@AppartmentNumber" & i, address.AppartmentNumber.FirstOrDefault(),
                                                 "@Street" & i, address.Street.FirstOrDefault(),
                                                 "@Locality" & i, address.Locality.FirstOrDefault(),
                                                 "@Region" & i, address.Region.FirstOrDefault(),
                                                 "@PostalCode" & i, address.PostalCode.FirstOrDefault(),
                                                 "@Country" & i, address.Country.FirstOrDefault(),
                                                 "@PreferenceLevel" & i, GetPrefParameter(address),                   ' ADR PREF param
                                                 "@SortIndex" & i, address.RawProperty.SortIndex               ' Property position in vCard.
                                                 })
                ' Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("ADR", address.RawProperty, addressId.ToString(), uid, clientAppName, customPropSqlInsert, customPropParametersInsert) Then
                    sql += "; " & customPropSqlInsert
                    parameters.AddRange(customPropParametersInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        ''' <summary>
        ''' Saves data to [card_InstantMessenger] table.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="instantMessengers">List of instant messengers to be saved.</param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        Private Async Function WriteInstantMessengersAsync(context As DavContext, instantMessengers As ITextPropertyList(Of IInstantMessenger3), uid As String, clientAppName As String) As Task
            Dim sql As String = "INSERT INTO [card_InstantMessenger] (
                      [InstantMessengerId]
                    , [UID]
                    , [Type]
                    , [InstantMessenger]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each instantMessenger As IInstantMessenger3 In instantMessengers
                valuesSql.Add(String.Format("(
                      @InstantMessengerId{0}
                    , @UID
                    , @Type{0}
                    , @InstantMessenger{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i))
                Dim instantMessengerId As Guid = Guid.NewGuid()
                parameters.AddRange(New Object() {"@InstantMessengerId" & i, instantMessengerId,
                                                 "@Type" & i, ListToString(Of MessengerType)(instantMessenger.Types), ' IMPP TYPE param
                                                 "@InstantMessenger" & i, instantMessenger.Text,                               ' IMPP VALUE
                                                 "@PreferenceLevel" & i, GetPrefParameter(instantMessenger),                  ' IMPP PREF param
                                                 "@SortIndex" & i, instantMessenger.RawProperty.SortIndex              ' Property position in vCard.
                                                 })
                ' Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("IMPP", instantMessenger.RawProperty, instantMessengerId.ToString(), uid, clientAppName, customPropSqlInsert, customPropParametersInsert) Then
                    sql += "; " & customPropSqlInsert
                    parameters.AddRange(customPropParametersInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        ''' <summary>
        ''' Saves data to [card_Telephone] table.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="instantMessengers">List of telephones to be saved.</param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        Private Async Function WriteTelephonesAsync(context As DavContext, telephones As ITextPropertyList(Of ITelephone2), uid As String, clientAppName As String) As Task
            Dim sql As String = "INSERT INTO [card_Telephone] (
                      [TelephoneId]
                    , [UID]
                    , [Type]
                    , [Telephone]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each telephone As ITelephone2 In telephones
                valuesSql.Add(String.Format("(
                      @TelephoneId{0}
                    , @UID
                    , @Type{0}
                    , @Telephone{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i))
                Dim telephoneId As Guid = Guid.NewGuid()
                parameters.AddRange(New Object() {"@TelephoneId" & i, telephoneId,
                                                 "@Type" & i, ListToString(Of TelephoneType)(telephone.Types),' TEL TYPE param
                                                 "@Telephone" & i, telephone.Text,                              ' TEL VALUE
                                                 "@PreferenceLevel" & i, GetPrefParameter(telephone),                 ' TEL PREF param
                                                 "@SortIndex" & i, telephone.RawProperty.SortIndex             ' Property position in vCard.
                                                 })
                ' Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("TEL", telephone.RawProperty, telephoneId.ToString(), uid, clientAppName, customPropSqlInsert, customPropParametersInsert) Then
                    sql += "; " & customPropSqlInsert
                    parameters.AddRange(customPropParametersInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        ''' <summary>
        ''' Saves data to [card_Url] table.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="url">List of URLs to be saved.</param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        Private Async Function WriteUrlsAsync(context As DavContext, urls As ICardPropertyList(Of ICardUriProperty2), uid As String, clientAppName As String) As Task
            Dim sql As String = "INSERT INTO [card_Url] (
                      [UrlId]
                    , [UID]
                    , [Type]
                    , [Url]
                    , [PreferenceLevel]
                    , [SortIndex]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            Dim parameters As List(Of Object) = New List(Of Object)(New Object() {"@UID", uid
                                                                                 })
            Dim i As Integer = 0
            For Each url As ICardUriProperty2 In urls
                valuesSql.Add(String.Format("(
                      @UrlId{0}
                    , @UID
                    , @Type{0}
                    , @Url{0}
                    , @PreferenceLevel{0}
                    , @SortIndex{0}
                )", i))
                Dim urlId As Guid = Guid.NewGuid()
                parameters.AddRange(New Object() {"@UrlId" & i, urlId,
                                                 "@Type" & i, ListToString(Of ExtendibleEnum)(url.Types), ' TEL TYPE param 
                                                 "@Url" & i, url.Text,                                ' URL VALUE
                                                 "@PreferenceLevel" & i, GetPrefParameter(url),                   ' URL PREF param
                                                 "@SortIndex" & i, url.RawProperty.SortIndex               ' Property position in vCard.
                                                 })
                ' Prepare SQL to save custom property parameters to [card_CustomProperty] table.
                Dim customPropSqlInsert As String
                Dim customPropParametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty("URL", url.RawProperty, urlId.ToString(), uid, clientAppName, customPropSqlInsert, customPropParametersInsert) Then
                    sql += "; " & customPropSqlInsert
                    parameters.AddRange(customPropParametersInsert)
                End If

                i += 1
            Next

            If i > 0 Then
                Await context.ExecuteNonQueryAsync(String.Format(sql, String.Join(", ", valuesSql.ToArray())), parameters.ToArray())
            End If
        End Function

        ''' <summary>
        ''' Gets 1 if property is preferred in case of vCard 2.1 &  3.0. Returns preference level in case of vCard 4.0.
        ''' </summary>
        ''' <param name="prop">Card property to get prefference from.</param>
        ''' <returns>Integer between 1 and 100 or null if PREF property is not specified.</returns>
        Private Shared Function GetPrefParameter(prop As ICardMultiProperty) As Byte?
            Dim prop4 As ICardMultiProperty4 = TryCast(prop, ICardMultiProperty4)
            If prop4 Is Nothing Then
                Return If(prop.IsPrefered, CType(1, Byte?), Nothing)
            End If

            Return CType(prop4.PreferenceLevel, Byte?)
        End Function

        ''' <summary>
        ''' Creates SQL to write custom properties and parameters to [card_CustomProperty] table.
        ''' </summary>
        ''' <param name="prop">Raw property to be saved to database.</param>
        ''' <param name="parentId">
        ''' Parent component ID or parent property ID to which this custom property or parameter belongs to. 
        ''' This could be UID (in case of [card_CardFile] table), EmailId, InstantMessengerId, etc.
        ''' </param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        ''' <param name="sql">SQL to insert data to DB.</param>
        ''' <param name="parameters">SQL parameter values that will be filled by this method.</param>
        ''' <returns>True if any custom properies or parameters found, false otherwise.</returns>
        Private Function PrepareSqlParamsWriteCustomProperty(propName As String, prop As IRawProperty, parentId As String, uid As String, clientAppName As String, ByRef sql As String, ByRef parameters As List(Of Object)) As Boolean
            sql = "INSERT INTO [card_CustomProperty] (
                      [ParentId]
                    , [UID]
                    , [ClientAppName]
                    , [PropertyName]
                    , [ParameterName]
                    , [Value]
                    , [SortIndex]
                ) VALUES {0}"
            Dim valuesSql As List(Of String) = New List(Of String)()
            parameters = New List(Of Object)()
            Dim origParamsCount As Integer = parameters.Count()
            ' Custom properties are one of the following:
            '  - props that start with "X-". This is a standard-based approach to creating custom props.
            '  - props that has "." in its name. Typically "item1.X-PROP". Such props are created by iOS and OS X.
            Dim isCustomProp As Boolean = propName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase) OrElse propName.Contains(".")
            Dim paramName As String = Nothing
            ' Save custom prop value.
            If isCustomProp Then
                Dim val As String = prop.RawValue
                valuesSql.Add(String.Format("(
                                  @ParentId{0}
                                , @UID{0}
                                , @ClientAppName{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                , @SortIndexParam{0}
                                )", paramIndex))
                parameters.AddRange(New Object() {"@ParentId" & paramIndex, parentId,
                                                 "@UID" & paramIndex, uid,              ' Added for performance optimization purposes.
                                                 "@ClientAppName" & paramIndex, clientAppName,    ' Client app name that created this custom property.
                                                 "@PropertyName" & paramIndex, propName,
                                                 "@ParameterName" & paramIndex, paramName,        ' null is inserted to mark prop value.
                                                 "@Value" & paramIndex, val,
                                                 "@SortIndexParam" & paramIndex, prop.SortIndex   ' Property position in vCard.
                                                 })
                paramIndex += 1
            End If

            ' Save parameters and their values.
            For Each param As Parameter In prop.Parameters
                paramName = param.Name
                ' For standard properties we save only custom params (that start with 'X-'). All standard params go to their fields in DB.
                ' For custom properies we save all params.
                If Not isCustomProp AndAlso Not paramName.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase) Then Continue For
                For Each value As String In param.Values
                    Dim val As String = value
                    valuesSql.Add(String.Format("(
                                  @ParentId{0}
                                , @UID{0}
                                , @ClientAppName{0}
                                , @PropertyName{0}
                                , @ParameterName{0}
                                , @Value{0}
                                , @SortIndexParam{0}
                                )", paramIndex))
                    parameters.AddRange(New Object() {"@ParentId" & paramIndex, parentId,
                                                     "@UID" & paramIndex, uid,          ' added for performance optimization purposes
                                                     "@ClientAppName" & paramIndex, clientAppName,' Client app name that created this custom parameter.
                                                     "@PropertyName" & paramIndex, propName,
                                                     "@ParameterName" & paramIndex, paramName,
                                                     "@Value" & paramIndex, val,
                                                     "@SortIndexParam" & paramIndex, Nothing})
                    paramIndex += 1
                Next
            Next

            If origParamsCount < parameters.Count() Then
                sql = String.Format(sql, String.Join(", ", valuesSql.ToArray()))
                Return True
            End If

            Return False
        End Function

        ''' <summary>
        ''' Creates SQL to write custom properties and parameters to [card_CustomProperty] table for specified component.
        ''' </summary>
        ''' <param name="component">Component to be saved to database.</param>
        ''' <param name="parentId">
        ''' Parent component ID to which this custom property or parameter belongs to. 
        ''' This could be UID (in case of [card_CardFile] table), EmailId, InstantMessengerId, etc.
        ''' </param>
        ''' <param name="uid">Card UID.</param>
        ''' <param name="clientAppName">Used to keep custom props created by this CardDAV client when updated by other CardDAV clients.</param>
        ''' <param name="sql">SQL to insert data to DB.</param>
        ''' <param name="parameters">SQL parameter values that will be filled by this method.</param>
        ''' <returns>True if any custom properies or parameters found, false otherwise.</returns>
        Private Function PrepareSqlCustomPropertiesOfComponentAsync(component As IComponent, parentId As String, uid As String, clientAppName As String, ByRef sql As String, ByRef parameters As List(Of Object)) As Boolean
            sql = ""
            parameters = New List(Of Object)()
            ' We save only single custom props here, multiple props are saved in other methods.
            Dim multiProps As String() = New String() {"EMAIL", "ADR", "IMPP", "TEL", "URL"}
            ' Properties in IComponent.Properties are grouped by name.
            For Each pair As KeyValuePair(Of String, IList(Of IRawProperty)) In component.Properties
                If multiProps.Contains(pair.Key.ToUpper()) OrElse (pair.Value.Count <> 1) Then Continue For
                Dim sqlInsert As String
                Dim parametersInsert As List(Of Object)
                If PrepareSqlParamsWriteCustomProperty(pair.Key, pair.Value.First(), parentId, uid, clientAppName, sqlInsert, parametersInsert) Then
                    sql += "; " & sqlInsert
                    parameters.AddRange(parametersInsert)
                End If
            Next

            Return Not String.IsNullOrEmpty(sql)
        End Function

        ''' <summary>
        ''' Called when client application deletes this file.
        ''' </summary>
        ''' <param name="multistatus">Error description if case delate failed. Ignored by most clients.</param>
        Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            Dim sql As String = "DELETE FROM [card_CardFile] 
                           WHERE FileName=@FileName
                           AND [AddressbookFolderId] IN (SELECT [AddressbookFolderId] FROM [card_Access] WHERE [UserId]=@UserId AND [Write] = 1)"
            If Await Context.ExecuteNonQueryAsync(sql, 
                                                 "@UserId", Context.UserId,
                                                 "@FileName", fileName) < 1 Then
                Throw New DavException("Item not found or you do not have enough permissions to complete this operation.", DavStatus.FORBIDDEN)
            End If
        End Function

        ''' <summary>
        ''' Called when a card must be read from back-end storage.
        ''' </summary>
        ''' <param name="output">Stream to write vCard content.</param>
        ''' <param name="startIndex">Index to start reading data from back-end storage. Used for segmented reads, not used by CardDAV clients.</param>
        ''' <param name="count">Number of bytes to read. Used for segmented reads, not used by CardDAV clients.</param>
        Public Async Function ReadAsync(output As Stream, startIndex As Long, count As Long) As Task Implements IContentAsync.ReadAsync
            Dim vCardVersion As String = rowCardFile.Field(Of String)("Version")
            Dim card As ICard2 = CardFactory.CreateCard(vCardVersion)
            ReadCard(card)
            ReadEmails(card.Emails, rowsEmails)
            ReadAddresses(card.Addresses, rowsAddresses)
            ReadTelephones(card.Telephones, rowsTelephones)
            ReadUrls(card.Urls, rowsUrls)
            ' IMPP is vCard 3.0 & 4.0 prop
            Dim card3 As ICard3 = TryCast(card, ICard3)
            If card3 IsNot Nothing Then
                ReadMessengers(card3.InstantMessengers, rowsInstantMessengers)
            End If

            ' Check if this CardDAV client application requires properties conversion.
            If AppleCardInteroperability.NeedsConversion(Context.Request.UserAgent) Then
                ' In case of iOS & OS X the props below must be converted to the following format:
                ' item2.TEL:(222)222 - 2222
                ' item2.X-ABLabel:Emergency
                AppleCardInteroperability.Denormalize(card)
            End If

            Call New vFormatter().Serialize(output, card)
        End Function

        ''' <summary>
        ''' Reads data from [card_CardFile] row.
        ''' </summary>
        ''' <param name="card">Card that will be populated from row paramater.</param>
        Private Sub ReadCard(card As ICard2)
            Dim uid As String = rowCardFile.Field(Of String)("UID")
            'UID
            card.Uid = card.CreateTextProp(uid)
            ' PRODID
            card.ProductId = card.CreateTextProp("-//IT Hit//Collab Lib//EN")
            ' FN
            card.FormattedNames.Add(rowCardFile.Field(Of String)("FormattedName"))
            ' N
            card.Name = card.CreateNameProp(rowCardFile.Field(Of String)("FamilyName"),
                                           rowCardFile.Field(Of String)("GivenName"),
                                           rowCardFile.Field(Of String)("AdditionalNames"),
                                           rowCardFile.Field(Of String)("HonorificPrefix"),
                                           rowCardFile.Field(Of String)("HonorificSuffix"))
            ' PHOTO
            If Not rowCardFile.IsNull("Photo") Then
                card.Photos.Add(Convert.ToBase64String(rowCardFile.Field(Of Byte())("Photo")), rowCardFile.Field(Of String)("PhotoMediaType"), False)
            End If

            ' LOGO
            If Not rowCardFile.IsNull("Logo") Then
                card.Photos.Add(Convert.ToBase64String(rowCardFile.Field(Of Byte())("Logo")), rowCardFile.Field(Of String)("LogoMediaType"), False)
            End If

            ' SOUND
            If Not rowCardFile.IsNull("Sound") Then
                card.Photos.Add(Convert.ToBase64String(rowCardFile.Field(Of Byte())("Sound")), rowCardFile.Field(Of String)("SoundMediaType"), False)
            End If

            ' BDAY
            Dim birthday As DateTime? = rowCardFile.Field(Of DateTime?)("Birthday")
            If birthday IsNot Nothing Then
                card.BirthDate = card.CreateDateProp(birthday.Value, DateComponents.[Date])
            End If

            ' REV
            Dim revision As DateTime? = rowCardFile.Field(Of DateTime?)("RevisionUtc")
            If revision IsNot Nothing Then
                card.Revision = card.CreateDateProp(revision.Value)
            End If

            ' SORT-STRING
            Dim sortString As String = rowCardFile.Field(Of String)("SortString")
            If Not String.IsNullOrEmpty(sortString) Then
                Dim propSortString As ITextProperty2 = card.CreateProperty(Of ITextProperty2)()
                propSortString.Text = sortString
                card.SortString = propSortString
            End If

            ' TZ
            Dim timeZone As String = rowCardFile.Field(Of String)("TimeZone")
            If Not String.IsNullOrEmpty(timeZone) Then
                card.TimeZones.Add(timeZone)
            End If

            ' GEO
            ' TITLE
            Dim title As String = rowCardFile.Field(Of String)("Title")
            If Not String.IsNullOrEmpty(title) Then
                card.Titles.Add(title)
            End If

            ' ROLE
            Dim role As String = rowCardFile.Field(Of String)("Role")
            If Not String.IsNullOrEmpty(role) Then
                card.Roles.Add(role)
            End If

            ' ORG
            Dim orgName As String = rowCardFile.Field(Of String)("OrgName")
            Dim orgUnit As String = rowCardFile.Field(Of String)("OrgUnit")
            If Not String.IsNullOrEmpty(orgName) OrElse Not String.IsNullOrEmpty(orgUnit) Then
                Dim propOrg As IOrganization2 = card.Organizations.CreateProperty()
                propOrg.Name = orgName
                propOrg.Units = {orgUnit}
                card.Organizations.Add(propOrg)
            End If

            ' NOTE
            Dim note As String = rowCardFile.Field(Of String)("Note")
            If Not String.IsNullOrEmpty(note) Then
                card.Notes.Add(note)
            End If

            ' vCard v3.0 & v4.0 props
            If TypeOf card Is ICard3 Then
                Dim card3 As ICard3 = TryCast(card, ICard3)
                ' NICKNAME
                Dim nickname As String = rowCardFile.Field(Of String)("Nickname")
                If Not String.IsNullOrEmpty(nickname) Then
                    Dim propNickname As INickname3 = card3.NickNames.CreateProperty()
                    propNickname.Values = {nickname}
                    card3.NickNames.Add(propNickname)
                End If

                ' CATEGORIES
                Dim categories As String = rowCardFile.Field(Of String)("Categories")
                If Not String.IsNullOrEmpty(categories) Then
                    Dim aCategories As String() = categories.Split({";"c}, StringSplitOptions.RemoveEmptyEntries)
                    For Each categoryList As String In aCategories
                        Dim catProp As ICategories3 = card3.Categories.CreateProperty()
                        catProp.Values = categoryList.Split({","c}, StringSplitOptions.RemoveEmptyEntries)
                        card3.Categories.Add(catProp)
                    Next
                End If

                ' CLASS
                Dim classification As String = rowCardFile.Field(Of String)("Classification")
                If Not String.IsNullOrEmpty(classification) Then
                    card3.Classes.Add(classification)
                End If
            End If

            ' vCard v4.0 props
            If TypeOf card Is ICard4 Then
                Dim card4 As ICard4 = TryCast(card, ICard4)
                ' KIND
                Dim kind As String = rowCardFile.Field(Of String)("Kind")
                If kind IsNot Nothing Then
                    Dim propKind As IKind4 = card4.CreateProperty(Of IKind4)()
                    propKind.Text = kind
                    card4.Kind = propKind
                End If

                ' ANNIVERSARY
                Dim anniversary As DateTime? = rowCardFile.Field(Of DateTime?)("Anniversary")
                If anniversary IsNot Nothing Then
                    Dim propAnniversary As IAnniversary4 = card4.CreateProperty(Of IAnniversary4)()
                    propAnniversary.Value = New [Date](anniversary.Value, DateComponents.Month Or DateComponents.[Date])
                    card4.Anniversary = propAnniversary
                End If

                ' GENDER
                Dim gender As String = rowCardFile.Field(Of String)("Gender")
                If Not String.IsNullOrEmpty(gender) Then
                    Dim propGender As IGender4 = card4.CreateProperty(Of IGender4)()
                    propGender.Text = gender
                    card4.Gender = propGender
                End If

                ' LANG
                Dim language As String = rowCardFile.Field(Of String)("Language")
                If Not String.IsNullOrEmpty(language) Then
                    card4.ContactLanguages.Add(language)
                End If
            End If

            ' Get custom properties and custom parameters
            Dim rowsCardCustomProperties As IEnumerable(Of DataRow) = rowsCustomProperties.Where(Function(x) x.Field(Of String)("ParentId") = uid)
            ReadCustomProperties(card, rowsCardCustomProperties)
        End Sub

        ''' <summary>
        ''' Reads data from [card_Email] rows.
        ''' </summary>
        ''' <param name="emails">Empty emails list that will be populated with data from <paramref name="rowsEmails"/>  parameter.</param>
        ''' <param name="rowsEmails">Data from [card_Email] table to populate <paramref name="emails"/>  parameter.</param>
        Private Sub ReadEmails(emails As ITextPropertyList(Of IEmail2), rowsEmails As IEnumerable(Of DataRow))
            For Each rowEmail As DataRow In rowsEmails
                Dim email As IEmail2 = emails.CreateProperty()
                email.Text = rowEmail.Field(Of String)("Email")
                email.Types = ParseType(Of EmailType)(rowEmail.Field(Of String)("Type"))
                SetPrefParameter(email, rowEmail.Field(Of Byte?)("PreferenceLevel"))
                email.RawProperty.SortIndex = rowEmail.Field(Of Integer)("SortIndex")
                AddParamValues(rowEmail.Field(Of Guid)("EmailId"), email.RawProperty)
                emails.Add(email)
            Next
        End Sub

        ''' <summary>
        ''' Reads data from [card_Address] rows.
        ''' </summary>
        ''' <param name="addresses">Empty addresses list that will be populated with data from <paramref name="rowsAddresses"/>  parameter.</param>
        ''' <param name="rowsAddresses">Data from [card_Address] table to populate <paramref name="addresses"/>  parameter.</param>
        Private Sub ReadAddresses(addresses As ICardPropertyList(Of IAddress2), rowsAddresses As IEnumerable(Of DataRow))
            For Each rowAddress As DataRow In rowsAddresses
                Dim address As IAddress2 = addresses.CreateProperty()
                address.SetAddress({rowAddress.Field(Of String)("PoBox")},
                                  {rowAddress.Field(Of String)("AppartmentNumber")},
                                  {rowAddress.Field(Of String)("Street")},
                                  {rowAddress.Field(Of String)("Locality")},
                                  {rowAddress.Field(Of String)("Region")},
                                  {rowAddress.Field(Of String)("PostalCode")},
                                  {rowAddress.Field(Of String)("Country")},
                                  ParseType(Of AddressType)(rowAddress.Field(Of String)("Type")))
                SetPrefParameter(address, rowAddress.Field(Of Byte?)("PreferenceLevel"))
                address.RawProperty.SortIndex = rowAddress.Field(Of Integer)("SortIndex")
                AddParamValues(rowAddress.Field(Of Guid)("AddressId"), address.RawProperty)
                addresses.Add(address)
            Next
        End Sub

        ''' <summary>
        ''' Reads data from [card_InstantMessenger] rows.
        ''' </summary>
        ''' <param name="messengers">Empty emails list that will be populated with data from <paramref name="rowsMessengers"/>  parameter.</param>
        ''' <param name="rowsMessengers">Data from [card_InstantMessenger] table to populate <paramref name="messengers"/>  parameter.</param>
        Private Sub ReadMessengers(messengers As ITextPropertyList(Of IInstantMessenger3), rowsMessengers As IEnumerable(Of DataRow))
            For Each rowMessenger As DataRow In rowsMessengers
                Dim messenger As IInstantMessenger3 = messengers.CreateProperty()
                messenger.Text = rowMessenger.Field(Of String)("InstantMessenger")
                messenger.Types = ParseType(Of MessengerType)(rowMessenger.Field(Of String)("Type"))
                SetPrefParameter(messenger, rowMessenger.Field(Of Byte?)("PreferenceLevel"))
                messenger.RawProperty.SortIndex = rowMessenger.Field(Of Integer)("SortIndex")
                AddParamValues(rowMessenger.Field(Of Guid)("InstantMessengerId"), messenger.RawProperty)
                messengers.Add(messenger)
            Next
        End Sub

        ''' <summary>
        ''' Reads data from [card_Telephone] rows.
        ''' </summary>
        ''' <param name="telephones">Empty emails list that will be populated with data from <paramref name="rowsTelephones"/>  parameter.</param>
        ''' <param name="rowsTelephones">Data from [card_Telephone] table to populate <paramref name="telephones"/>  parameter.</param>
        Private Sub ReadTelephones(telephones As ITextPropertyList(Of ITelephone2), rowsTelephones As IEnumerable(Of DataRow))
            For Each rowTelephone As DataRow In rowsTelephones
                Dim telephone As ITelephone2 = telephones.CreateProperty()
                telephone.Text = rowTelephone.Field(Of String)("Telephone")
                telephone.Types = ParseType(Of TelephoneType)(rowTelephone.Field(Of String)("Type"))
                SetPrefParameter(telephone, rowTelephone.Field(Of Byte?)("PreferenceLevel"))
                telephone.RawProperty.SortIndex = rowTelephone.Field(Of Integer)("SortIndex")
                AddParamValues(rowTelephone.Field(Of Guid)("TelephoneId"), telephone.RawProperty)
                telephones.Add(telephone)
            Next
        End Sub

        ''' <summary>
        ''' Reads data from [card_Url] rows.
        ''' </summary>
        ''' <param name="urls">Empty URLs list that will be populated with data from <paramref name="rowsUrls"/>  parameter.</param>
        ''' <param name="rowsUrls">Data from [card_Url] table to populate <paramref name="urls"/>  parameter.</param>
        Private Sub ReadUrls(urls As ICardPropertyList(Of ICardUriProperty2), rowsUrls As IEnumerable(Of DataRow))
            For Each rowUrl As DataRow In rowsUrls
                Dim url As ICardUriProperty2 = urls.CreateProperty()
                url.Text = rowUrl.Field(Of String)("Url")
                url.Types = ParseType(Of ExtendibleEnum)(rowUrl.Field(Of String)("Type"))
                SetPrefParameter(url, rowUrl.Field(Of Byte?)("PreferenceLevel"))
                url.RawProperty.SortIndex = rowUrl.Field(Of Integer)("SortIndex")
                AddParamValues(rowUrl.Field(Of Guid)("UrlId"), url.RawProperty)
                urls.Add(url)
            Next
        End Sub

        ''' <summary>
        ''' Sets PREF parameter.
        ''' </summary>
        ''' <param name="prop">Property to set the PREF parameter.</param>
        ''' <param name="preferenceLevel">Preference level. If null is passed PREF is not set.</param>
        Private Shared Sub SetPrefParameter(prop As ICardMultiProperty, preferenceLevel As Byte?)
            If preferenceLevel IsNot Nothing Then
                Dim prop4 As ICardMultiProperty4 = TryCast(prop, ICardMultiProperty4)
                If prop4 Is Nothing Then
                    ' vCard 2.1 & 3.0
                    prop.IsPrefered = True
                Else
                    ' vCard 4.0
                    prop4.PreferenceLevel = preferenceLevel.Value
                End If
            End If
        End Sub

        ''' <summary>
        ''' Parses TYPE parameter.
        ''' </summary>
        ''' <param name="typesList">Coma separated list of types.</param>
        Private Shared Function ParseType(Of T As {ExtendibleEnum, New})(typesList As String) As T()
            If Not String.IsNullOrEmpty(typesList) Then
                Dim aStrTypes As String() = typesList.Split({","c}, StringSplitOptions.RemoveEmptyEntries)
                Return aStrTypes.Select(Function(x) StringToEnum(Of T)(x)).ToArray()
            End If

            Return New T() {}
        End Function

        ''' <summary>
        ''' Reads custom properties and parameters from [card_CustomProperty] table
        ''' and creates them in component passed as a parameter.
        ''' </summary>
        ''' <param name="component">Component where custom properties and parameters will be created.</param>
        ''' <param name="rowsCustomProperies">Custom properties datat from [card_CustomProperty] table.</param>
        Private Shared Sub ReadCustomProperties(component As IComponent, rowsCustomProperies As IEnumerable(Of DataRow))
            For Each rowCustomProperty As DataRow In rowsCustomProperies
                Dim propertyName As String = rowCustomProperty.Field(Of String)("PropertyName")
                Dim prop As IRawProperty
                If Not component.Properties.ContainsKey(propertyName) Then
                    prop = component.CreateRawProperty()
                    component.AddProperty(propertyName, prop)
                Else
                    prop = component.Properties(propertyName).FirstOrDefault()
                End If

                Dim paramName As String = rowCustomProperty.Field(Of String)("ParameterName")
                Dim value As String = rowCustomProperty.Field(Of String)("Value")
                If paramName Is Nothing Then
                    ' If ParameterName is null the Value contains property value
                    prop.RawValue = value
                    prop.SortIndex = rowCustomProperty.Field(Of Integer)("SortIndex")
                Else
                    prop.Parameters.Add(New Parameter(paramName, value))
                End If
            Next
        End Sub

        ''' <summary>
        ''' Adds custom parameters to property.
        ''' </summary>
        ''' <param name="propertyId">ID from [card_CardFile], [card_Email], [card_Address], [card_Telephone], [card_InstantMessenger] tables. Used to find parameters in [CardCustomProperties] table.</param>
        ''' <param name="prop">Property to add parameters to.</param>
        Private Sub AddParamValues(propertyId As Guid, prop As IRawProperty)
            Dim rowsCustomParams As IEnumerable(Of DataRow) = rowsCustomProperties.Where(Function(x) x.Field(Of String)("ParentId") = propertyId.ToString())
            For Each rowCustomParam As DataRow In rowsCustomParams
                Dim paramName As String = rowCustomParam.Field(Of String)("ParameterName")
                Dim paramValue As String = rowCustomParam.Field(Of String)("Value")
                prop.Parameters.Add(New Parameter(paramName, paramValue))
            Next
        End Sub

        ''' <summary>
        ''' Converts string to <see cref="ExtendibleEnum"/>  of spcified type. Returns <b>null</b> if <b>null</b> is passed. 
        ''' If no matching string value is found the <see cref="ExtendibleEnum.Name"/>  is set to passed parameter <b>value</b> and <see cref="ExtendibleEnum.Number"/>  is set to -1.
        ''' </summary>
        ''' <typeparam name="T">Type to convert to.</typeparam>
        ''' <param name="value">String to convert from.</param>
        ''' <returns><see cref="ExtendibleEnum"/>  of type <b>T</b> or <b>null</b> if <b>null</b> is passed as a parameter.</returns>
        Private Shared Function StringToEnum(Of T As {ExtendibleEnum, New})(value As String) As T
            If value Is Nothing Then Return Nothing
            Dim res As T
            If Not ExtendibleEnum.TryFromString(Of T)(value, res) Then
                ' If no matching value is found create new ExtendibleEnum or type T 
                ' with specified string value and default numeric value (-1).
                res = New T()
                res.Name = value
            End If

            Return res
        End Function
    End Class
End Namespace
