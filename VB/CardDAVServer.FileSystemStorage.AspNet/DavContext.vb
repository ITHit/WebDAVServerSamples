Imports System
Imports System.DirectoryServices.AccountManagement
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
Imports CardDAVServer.FileSystemStorage.AspNet.Acl
Imports CardDAVServer.FileSystemStorage.AspNet.CardDav
Imports CardDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

''' <summary>
''' Implementation of <see cref="DavContextBaseAsync"/> .
''' Resolves hierarchy items by paths.
''' </summary>
Public Class DavContext
    Inherits DavContextWebBaseAsync
    Implements IDisposable

    ''' <summary>
    ''' Disk full windows error code.
    ''' </summary>
    Private Const ERROR_DISK_FULL As Integer = 112

    ''' <summary>
    ''' A <see cref="PrincipalContext"/>  to be used for windows users operations.
    ''' </summary>
    Private principalContext As PrincipalContext

    ''' <summary>
    ''' Path to the folder which become available via WebDAV.
    ''' </summary>
    Public Property RepositoryPath As String

    ''' <summary>
    ''' Gets WebDAV Logger instance.
    ''' </summary>
    Public Property Logger As ILogger

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
    Public ReadOnly Property WindowsIdentity As WindowsIdentity
        Get
            Dim winIdentity As WindowsIdentity = TryCast(Identity, WindowsIdentity)
            If winIdentity IsNot Nothing AndAlso Not winIdentity.IsAnonymous Then
                Return winIdentity
            End If

            If AnonymousUser IsNot Nothing Then
                Return AnonymousUser
            End If

            Throw New Exception("Anonymous user is not configured.")
        End Get
    End Property

    ''' <summary>
    ''' Currently logged in identity.
    ''' </summary>
    Public Property Identity As IIdentity

    ''' <summary>
    ''' Gets domain of currently logged in user.
    ''' </summary>
    Public ReadOnly Property Domain As String
        Get
            Dim i As Integer = WindowsIdentity.Name.IndexOf("\")
            Return If(i > 0, WindowsIdentity.Name.Substring(0, i), Environment.MachineName)
        End Get
    End Property

    ''' <summary>
    ''' Gets or sets user configured as anonymous.
    ''' </summary>
    Public Property AnonymousUser As WindowsIdentity

    ''' <summary>
    ''' Retrieves or creates <see cref="PrincipalContext"/>  to be used for user related operations.
    ''' </summary>
    ''' <returns>Instance of <see cref="PrincipalContext"/> .</returns>
    Public Function GetPrincipalContext() As PrincipalContext
        If principalContext Is Nothing Then
            If String.IsNullOrEmpty(Domain) OrElse Environment.MachineName.Equals(Domain, StringComparison.InvariantCultureIgnoreCase) Then
                principalContext = New PrincipalContext(ContextType.Machine, Domain)
            Else
                principalContext = New PrincipalContext(ContextType.Domain)
            End If
        End If

        Return principalContext
    End Function

    ''' <summary>
    ''' Performs operation which creates, deletes or modifies user or group.
    ''' Performs impersonification and exception handling.
    ''' </summary>
    ''' <param name="action">Action which performs action with a user or group.</param>
    Public Sub PrincipalOperation(action As Action)
        Using impersonate()
            Try
                action()
            Catch ex As PrincipalOperationException
                Logger.LogError("Principal operation failed", ex)
                Throw New DavException("Principal operation failed", ex)
            End Try
        End Using
    End Sub

    ''' <summary>
    ''' Performs operation which queries, creates, deletes or modifies user or group.
    ''' Performs impersonification and exception handling.
    ''' </summary>
    ''' <param name="func">Function to perform.</param>
    ''' <typeparam name="T">Type of operation result.</typeparam>
    ''' <returns>The value which <paramref name="func"/>  returned.</returns>
    Public Function PrincipalOperation(Of T)(func As Func(Of T)) As T
        Using impersonate()
            Try
                Return func()
            Catch ex As PrincipalOperationException
                Logger.LogError("Principal operation failed", ex)
                Throw New DavException("Principal operation failed", ex)
            End Try
        End Using
    End Function

    ''' <summary>
    ''' Initializes a new instance of the DavContext class.
    ''' </summary>
    ''' <param name="httpContext"><see cref="HttpContext"/>  instance.</param>
    Public Sub New(httpContext As HttpContext)
        MyBase.New(httpContext)
        Logger = CardDAVServer.FileSystemStorage.AspNet.Logger.Instance
        Dim configRepositoryPath As String =(If(ConfigurationManager.AppSettings("RepositoryPath"), String.Empty)).TrimEnd(Path.DirectorySeparatorChar)
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
            CardDAVServer.FileSystemStorage.AspNet.Logger.Instance.LogError("Repository path specified in Web.config is invalid.", Nothing)
        End If

        If httpContext.User IsNot Nothing Then Identity = httpContext.User.Identity
    End Sub

    ''' <summary>
    ''' Creates <see cref="IHierarchyItemAsync"/>  instance by path.
    ''' </summary>
    ''' <param name="path">Item relative path including query string.</param>
    ''' <returns>Instance of corresponding <see cref="IHierarchyItemAsync"/>  or null if item is not found.</returns>
    Public Overrides Async Function GetHierarchyItemAsync(path As String) As Task(Of IHierarchyItemBaseAsync)
        path = path.Trim({" "c, "/"c})
        'remove query string.
        Dim ind As Integer = path.IndexOf("?"c)
        If ind > -1 Then
            path = path.Remove(ind)
        End If

        Dim item As IHierarchyItemAsync = Nothing
        ' Return items from [DAVLocation]/acl/ folder and subfolders.
        item = Await AclFactory.GetAclItemAsync(Me, path)
        If item IsNot Nothing Then Return item
        ' Return items from [DAVLocation]/addressbooks/ folder and subfolders.
        item = CardDavFactory.GetCardDavItem(Me, path)
        If item IsNot Nothing Then Return item
        ' Return folder that corresponds to [DAVLocation] path. If no DavLocation is defined in config file this is a website root.
        item = DavLocationFolder.GetDavLocationFolder(Me, path)
        If item IsNot Nothing Then Return item
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

    ''' <summary>
    ''' Performs file system operation with translating exceptions to those expected by WebDAV engine.
    ''' </summary>
    ''' <param name="item">Item on which operation is performed.</param>
    ''' <param name="action">The action to be performed.</param>
    ''' <param name="privilege">Privilege which is needed to perform the operation. If <see cref="UnauthorizedAccessException"/>  is thrown
    ''' this method will convert it to <see cref="NeedPrivilegesException"/>  exception and specify this privilege in it.</param>
    Public Sub FileOperation(item As IHierarchyItemAsync, action As Action, privilege As Privilege)
        Try
            Using impersonate()
                action()
            End Using
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            ex.AddRequiredPrivilege(item.Path, privilege)
            Throw ex
        Catch ex As IOException
            Dim hr As Integer = Marshal.GetHRForException(ex)
            If hr = ERROR_DISK_FULL Then
                Throw New InsufficientStorageException()
            End If

            Throw New DavException(ex.Message, DavStatus.CONFLICT)
        End Try
    End Sub

    ''' <summary>
    ''' Performs file system operation with translating exceptions to those expected by WebDAV engine.
    ''' </summary>
    ''' <param name="item">Item on which operation is performed.</param>
    ''' <param name="action">The action to be performed.</param>
    ''' <param name="privilege">Privilege which is needed to perform the operation. If <see cref="UnauthorizedAccessException"/>  is thrown
    ''' this method will convert it to <see cref="NeedPrivilegesException"/>  exception and specify this privilege in it.</param>
    Public Async Function FileOperationAsync(item As IHierarchyItemAsync, actionAsync As Func(Of Task), privilege As Privilege) As Task
        Try
            Using impersonate()
                Await actionAsync()
            End Using
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            ex.AddRequiredPrivilege(item.Path, privilege)
            Throw ex
        Catch ex As IOException
            Dim hr As Integer = Marshal.GetHRForException(ex)
            If hr = ERROR_DISK_FULL Then
                Throw New InsufficientStorageException()
            End If

            Throw New DavException(ex.Message, DavStatus.CONFLICT)
        End Try
    End Function

    ''' <summary>
    ''' Performs file system operation with translating exceptions to those expected by WebDAV engine.
    ''' </summary>
    ''' <param name="item">Item on which operation is performed.</param>
    ''' <param name="func">The action to be performed.</param>
    ''' <param name="privilege">Privilege which is needed to perform the operation.
    ''' If <see cref="UnauthorizedAccessException"/>  is thrown  this method will convert it to
    ''' <see cref="NeedPrivilegesException"/>  exception and specify this privilege in it.</param>
    ''' <typeparam name="T">Type of operation's result.</typeparam>
    ''' <returns>Result returned by <paramref name="func"/> .</returns>
    Public Async Function FileOperationAsync(Of T)(item As IHierarchyItemAsync, actionAsync As Func(Of Task(Of T)), privilege As Privilege) As Task(Of T)
        Try
            Using impersonate()
                Return Await actionAsync()
            End Using
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            ex.AddRequiredPrivilege(item.Path, privilege)
            Throw ex
        Catch ex As IOException
            Dim hr As Integer = Marshal.GetHRForException(ex)
            If hr = ERROR_DISK_FULL Then
                Throw New InsufficientStorageException()
            End If

            Throw New DavException(ex.Message, DavStatus.CONFLICT)
        End Try
    End Function

    ''' <summary>
    ''' Performs file system operation with translating exceptions to those expected by WebDAV engine, except
    ''' <see cref="UnauthorizedAccessException"/>  which must be caught and translated manually.
    ''' </summary>        
    ''' <param name="action">The action to be performed.</param>
    Public Async Function FileOperationAsync(actionAsync As Func(Of Task)) As Task
        Try
            Using impersonate()
                Await actionAsync()
            End Using
        Catch ex As IOException
            Dim hr As Integer = Marshal.GetHRForException(ex)
            If hr = ERROR_DISK_FULL Then
                Throw New InsufficientStorageException()
            End If

            Throw New DavException(ex.Message, DavStatus.CONFLICT)
        End Try
    End Function

    ''' <summary>
    ''' Performs file system operation with translating exceptions to those expected by WebDAV engine, except
    ''' <see cref="UnauthorizedAccessException"/>  which must be caught and translated manually.
    ''' </summary>        
    ''' <param name="action">The action to be performed.</param>
    Public Sub FileOperation(action As Action)
        Try
            Using impersonate()
                action()
            End Using
        Catch ex As IOException
            Dim hr As Integer = Marshal.GetHRForException(ex)
            If hr = ERROR_DISK_FULL Then
                Throw New InsufficientStorageException()
            End If

            Throw New DavException(ex.Message, DavStatus.CONFLICT)
        End Try
    End Sub

    ''' <summary>
    ''' Performs file system operation with translating exceptions to those expected by WebDAV engine.
    ''' </summary>
    ''' <param name="item">Item on which operation is performed.</param>
    ''' <param name="func">The action to be performed.</param>
    ''' <param name="privilege">Privilege which is needed to perform the operation.
    ''' If <see cref="UnauthorizedAccessException"/>  is thrown  this method will convert it to
    ''' <see cref="NeedPrivilegesException"/>  exception and specify this privilege in it.</param>
    ''' <typeparam name="T">Type of operation's result.</typeparam>
    ''' <returns>Result returned by <paramref name="func"/> .</returns>
    Public Function FileOperation(Of T)(item As IHierarchyItemAsync, func As Func(Of T), privilege As Privilege) As T
        Try
            Using impersonate()
                Return func()
            End Using
        Catch __unusedUnauthorizedAccessException1__ As UnauthorizedAccessException
            Dim ex As NeedPrivilegesException = New NeedPrivilegesException("Not enough privileges")
            ex.AddRequiredPrivilege(item.Path, privilege)
            Throw ex
        Catch ex As IOException
            Dim hr As Integer = Marshal.GetHRForException(ex)
            If hr = ERROR_DISK_FULL Then
                Throw New InsufficientStorageException()
            End If

            Throw New DavException(ex.Message, DavStatus.CONFLICT)
        End Try
    End Function

    ''' <summary>
    ''' Dispose everything we have.
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        If principalContext IsNot Nothing Then
            principalContext.Dispose()
        End If
    End Sub

    ''' <summary>
    ''' Impersonates current user.
    ''' </summary>
    ''' <returns>Impersonation context, which must be disposed to 'unimpersonate'</returns>
    Private Function impersonate() As WindowsImpersonationContext
        Return LogonUtil.DuplicateToken(WindowsIdentity).Impersonate()
    End Function
End Class
