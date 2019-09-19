Imports System
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Collections.Generic
Imports System.Linq
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.Collab
Imports ITHit.Collab.Card
Imports ITHit.WebDAV.Server

Namespace CardDav

    ''' <summary>
    ''' Represents business card in an address book on a CardDAV server. Typically contains a single business card in vCard format.
    ''' Instances of this class correspond to the following path: [DAVLocation]/addressbooks/[user_name]/[addressbook_name]/[file_name].vcf.
    ''' </summary>
    ''' <example>
    ''' [DAVLocation]
    '''  |-- ...
    '''  |-- addressbooks
    '''      |-- ...
    '''      |-- [User2]
    '''           |-- [Address Book 1]
    '''           |-- ...
    '''           |-- [Address Book X]
    '''                |-- [File 1.vcf]  -- this class
    '''                |-- ...
    '''                |-- [File X.vcf]  -- this class
    ''' </example>
    Public Class CardFile
        Inherits DavFile
        Implements ICardFileAsync

        ''' <summary>
        ''' Returns business card file that corresponds to path.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        ''' <returns>CardFile instance or null if not found.</returns>
        Public Shared Function GetCardFile(context As DavContext, path As String) As CardFile
            Dim pattern As String = String.Format("^/?{0}/(?<user_name>[^/]+)/(?<addressbook_name>[^/]+)/(?<file_name>[^/]+\.vcf)$",
                                                 AddressbooksRootFolder.AddressbooksRootFolderPath.Trim(New Char() {"/"c}).Replace("/", "/?"))
            If Not Regex.IsMatch(path, pattern) Then Return Nothing
            Dim file As FileInfo = New FileInfo(context.MapPath(path))
            If Not file.Exists Then Return Nothing
            Return New CardFile(file, context, path)
        End Function

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CardFile"/>  class.
        ''' </summary>
        ''' <param name="file"><see cref="FileInfo"/>  for corresponding object in file system.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        Private Sub New(file As FileInfo, context As DavContext, path As String)
            MyBase.New(file, context, path)
        End Sub

        ''' <summary>
        ''' Called when a card must be read from back-end storage.
        ''' </summary>
        ''' <param name="output">Stream to write vCard content.</param>
        ''' <param name="startIndex">Index to start reading data from back-end storage. Used for segmented reads, not used by CardDAV clients.</param>
        ''' <param name="count">Number of bytes to read. Used for segmented reads, not used by CardDAV clients.</param>
        Public Overrides Async Function ReadAsync(output As Stream, startIndex As Long, count As Long) As Task Implements IContentAsync.ReadAsync
            Dim vCard As String = Nothing
            Using reader As StreamReader = File.OpenText(Me.fileSystemInfo.FullName)
                vCard = Await reader.ReadToEndAsync()
            End Using

            ' Typically the stream contains a single vCard.
            Dim cards As IEnumerable(Of IComponent) = New vFormatter().Deserialize(vCard)
            Dim card As ICard2 = TryCast(cards.First(), ICard2)
            ' We store a business card in the original vCard form sent by the CardDAV client app.
            ' This form may not be understood by some CardDAV client apps.
            ' Here we convert card to the form understood by the client CardDAV application.
            If AppleCardInteroperability.Convert(context.Request.UserAgent, card) Then
                Call New vFormatter().Serialize(output, card)
            Else
                ' No conversion is needed.
                Await MyBase.ReadAsync(output, startIndex, count)
            End If
        End Function

        ''' <summary>
        ''' Called when card is being saved to back-end storage.
        ''' </summary>
        ''' <param name="stream">Stream containing VCARD.</param>
        ''' <param name="contentType">Content type.</param>
        ''' <param name="startIndex">Starting byte in target file
        ''' for which data comes in <paramref name="content"/>  stream.</param>
        ''' <param name="totalFileSize">Size of file as it will be after all parts are uploaded. -1 if unknown (in case of chunked upload).</param>
        ''' <returns>Whether the whole stream has been written.</returns>
        Public Overrides Async Function WriteAsync(content As Stream, contentType As String, startIndex As Long, totalFileSize As Long) As Task(Of Boolean) Implements IContentAsync.WriteAsync
            ' We store a business card in the original vCard form sent by the CardDAV client app.
            ' This form may not be understood by some CardDAV client apps. 
            ' We will convert the card if needed when reading depending on the client app reading the vCard.
            Return Await MyBase.WriteAsync(content, contentType, startIndex, totalFileSize)
        End Function
    End Class
End Namespace
