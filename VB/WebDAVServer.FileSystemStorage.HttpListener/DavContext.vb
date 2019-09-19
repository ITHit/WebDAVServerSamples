Imports System
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Security.Principal
Imports System.Configuration
Imports System.Threading.Tasks
Imports ITHit.Server
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Quota
Imports WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes

''' <summary>
''' Implementation of <see cref="DavContextBaseAsync"/> .
''' Resolves hierarchy items by paths.
''' </summary>
Public Class DavContext
    Inherits ContextHttpListenerAsync(Of IHierarchyItemAsync)

    ''' <summary>
    ''' Path to the folder which become available via WebDAV.
    ''' </summary>
    Public Property RepositoryPath As String

    ''' <summary>
    ''' Gets WebDAV Logger instance.
    ''' </summary>
    Public Property Logger As ILogger

    ''' <summary>
    ''' Singleton instance of <see cref="WebSocketsService"/> .
    ''' </summary>
    Public Property socketService As WebSocketsService
        Get
            Return WebSocketsService.Service
        End Get

        Private Set(ByVal value As WebSocketsService)
        End Set
    End Property

    ''' <summary>
    ''' Gets user name.
    ''' </summary>
    ''' <remarks>In case of windows authentication returns user name without domain part.</remarks>
    Public ReadOnly Property UserName As String
        Get
            Dim i As Integer = Identity.Name.IndexOf("\")
            Return If(i > 0, Identity.Name.Substring(i + 1, Identity.Name.Length - i - 1), Identity.Name)
        End Get
    End Property

    ''' <summary>
    ''' Gets currently authenticated user.
    ''' </summary>
 
    ''' <summary>
    ''' Currently logged in identity.
    ''' </summary>
    Public Property Identity As IIdentity

    ''' <summary>
    ''' Initializes a new instance of the DavContext class.
    ''' </summary>
    ''' <param name="listenerContext"><see cref="HttpListenerContext"/>  instance.</param>
    ''' <param name="prefixes">Http listener prefixes.</param>
    ''' <param name="repositoryPath">Local path to repository.</param>
    ''' <param name="logger"><see cref="ILogger"/>  instance.</param>
    Public Sub New(listenerContext As HttpListenerContext,
                  prefixes As HttpListenerPrefixCollection,
                  principal As System.Security.Principal.IPrincipal,
                  repositoryPath As String,
                  logger As ILogger)
        MyBase.New(listenerContext, prefixes)
        Me.Logger = logger
        Me.RepositoryPath = repositoryPath
        If Not Directory.Exists(repositoryPath) Then
            Logger.LogError("Repository path specified in Web.config is invalid.", Nothing)
        End If

        If principal IsNot Nothing Then Identity = principal.Identity
    End Sub

    ''' <summary>
    ''' Creates <see cref="IHierarchyItemAsync"/>  instance by path.
    ''' </summary>
    ''' <param name="path">Item relative path including query string.</param>
    ''' <returns>Instance of corresponding <see cref="IHierarchyItemAsync"/>  or null if item is not found.</returns>
    Public Overrides Async Function GetHierarchyItemAsync(path As String) As Task(Of IHierarchyItemAsync)
        path = path.Trim({" "c, "/"c})
        'remove query string.
        Dim ind As Integer = path.IndexOf("?"c)
        If ind > -1 Then
            path = path.Remove(ind)
        End If

        Dim item As IHierarchyItemAsync = Nothing
        item = Await DavFolder.GetFolderAsync(Me, path)
        If item IsNot Nothing Then Return item
        item = Await DavFile.GetFileAsync(Me, path)
        If item IsNot Nothing Then Return item
        Logger.LogDebug("Could not find item that corresponds to path: " & path)
        Return Nothing
    End Function

    ''' <summary>
    ''' Returns the physical file path that corresponds to the specified virtual path on the Web server.
    ''' </summary>
    ''' <param name="relativePath">Path relative to WebDAV root folder.</param>
    ''' <returns>Corresponding path in file system.</returns>
    Friend Function MapPath(relativePath As String) As String
        'Convert to local file system path by decoding every part, reversing slashes and appending
        'to repository root.
        Dim encodedParts As String() = relativePath.Split({"/"}, StringSplitOptions.RemoveEmptyEntries)
        Dim decodedParts As String() = encodedParts.Select(Of String)(AddressOf EncodeUtil.DecodeUrlPart).ToArray()
        Return Path.Combine(RepositoryPath, String.Join(Path.DirectorySeparatorChar.ToString(), decodedParts))
    End Function
End Class
