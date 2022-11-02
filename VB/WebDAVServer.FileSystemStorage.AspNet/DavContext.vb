Imports System
Imports System.IO
Imports System.Linq
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Security.Principal
Imports System.Web
Imports System.Configuration
Imports System.Threading.Tasks
Imports ITHit.Server
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Quota
Imports ILogger = ITHit.Server.ILogger
Imports WebDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

''' <summary>
''' Implementation of <see cref="ContextAsync{IHierarchyItem}"/> .
''' Resolves hierarchy items by paths.
''' </summary>
Public Class DavContext
    Inherits ContextWebAsync(Of IHierarchyItem)

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
    ''' Initializes a new instance of the DavContext class.
    ''' </summary>
    ''' <param name="httpContext"><see cref="HttpContext"/>  instance.</param>
    Public Sub New(httpContext As HttpContext)
        MyBase.New(httpContext)
        Logger = WebDAVServer.FileSystemStorage.AspNet.Logger.Instance
        RepositoryPath = If(ConfigurationManager.AppSettings("RepositoryPath"), String.Empty)
        Dim isRoot As Boolean = New DirectoryInfo(RepositoryPath).Parent Is Nothing
        Dim configRepositoryPath As String = If(isRoot, RepositoryPath, RepositoryPath.TrimEnd(Path.DirectorySeparatorChar))
        RepositoryPath = If(configRepositoryPath.StartsWith("~"), HttpContext.Current.Server.MapPath(configRepositoryPath), configRepositoryPath)
        Dim attrStoragePath As String =(If(ConfigurationManager.AppSettings("AttrStoragePath"), String.Empty)).TrimEnd(Path.DirectorySeparatorChar)
        attrStoragePath = If(attrStoragePath.StartsWith("~"), HttpContext.Current.Server.MapPath(attrStoragePath), attrStoragePath)
        If Not FileSystemInfoExtension.IsUsingFileSystemAttribute Then
            If Not String.IsNullOrEmpty(attrStoragePath) Then
                FileSystemInfoExtension.UseFileSystemAttribute(New FileSystemExtendedAttribute(attrStoragePath, Me.RepositoryPath))
            ElseIf Not(New DirectoryInfo(RepositoryPath).IsExtendedAttributesSupported()) Then
                Dim tempPath = Path.Combine(Path.GetTempPath(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)
                FileSystemInfoExtension.UseFileSystemAttribute(New FileSystemExtendedAttribute(tempPath, Me.RepositoryPath))
            End If
        End If

        If Not Directory.Exists(RepositoryPath) Then
            WebDAVServer.FileSystemStorage.AspNet.Logger.Instance.LogError("Repository path specified in Web.config is invalid.", Nothing)
        End If
    End Sub

    ''' <summary>
    ''' Creates <see cref="IHierarchyItem"/>  instance by path.
    ''' </summary>
    ''' <param name="path">Item relative path including query string.</param>
    ''' <returns>Instance of corresponding <see cref="IHierarchyItem"/>  or null if item is not found.</returns>
    Public Overrides Async Function GetHierarchyItemAsync(path As String) As Task(Of IHierarchyItem)
        path = path.Trim({" "c, "/"c})
        'remove query string.
        Dim ind As Integer = path.IndexOf("?"c)
        If ind > -1 Then
            path = path.Remove(ind)
        End If

        Dim item As IHierarchyItem = Nothing
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
