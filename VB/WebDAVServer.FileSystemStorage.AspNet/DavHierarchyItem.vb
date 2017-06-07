Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Class2
Imports ITHit.WebDAV.Server.MicrosoftExtensions
Imports WebDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

''' <summary>
''' Base class for WebDAV items (folders, files, etc).
''' </summary>
Public MustInherit Class DavHierarchyItem
    Implements IHierarchyItemAsync, ILockAsync, IMsItemAsync

    ''' <summary>
    ''' Property name to return text anound search phrase.
    ''' </summary>
    Friend Const snippetProperty As String = "snippet"

    ''' <summary>
    ''' Name of properties attribute.
    ''' </summary>
    Friend Const propertiesAttributeName As String = "Properties"

    ''' <summary>
    ''' Name of locks attribute.
    ''' </summary>
    Friend Const locksAttributeName As String = "Locks"

    ''' <summary>
    ''' Gets name of the item.
    ''' </summary>
    Public ReadOnly Property Name As String Implements IHierarchyItemAsync.Name
        Get
            Return fileSystemInfo.Name
        End Get
    End Property

    ''' <summary>
    ''' Gets date when the item was created in UTC.
    ''' </summary>
    Public ReadOnly Property Created As DateTime Implements IHierarchyItemAsync.Created
        Get
            Return fileSystemInfo.CreationTimeUtc
        End Get
    End Property

    ''' <summary>
    ''' Gets date when the item was last modified in UTC.
    ''' </summary>
    Public ReadOnly Property Modified As DateTime Implements IHierarchyItemAsync.Modified
        Get
            Return fileSystemInfo.LastWriteTimeUtc
        End Get
    End Property

    ''' <summary>
    ''' Gets path of the item where each part between slashes is encoded.
    ''' </summary>
    Public Property Path As String Implements IHierarchyItemAsync.Path

    ''' <summary>
    ''' Gets full path for this file/folder in the file system.
    ''' </summary>
    Public ReadOnly Property FullPath As String
        Get
            Return fileSystemInfo.FullName.TrimEnd(System.IO.Path.DirectorySeparatorChar)
        End Get
    End Property

    ''' <summary>
    ''' Corresponding file or folder in the file system.
    ''' </summary>
    Friend fileSystemInfo As FileSystemInfo

    ''' <summary>
    ''' WebDAV Context.
    ''' </summary>
    Protected context As DavContext

    ''' <summary>
    ''' User defined property values.
    ''' </summary>
    Private propertyValues As List(Of PropertyValue)

    ''' <summary>
    ''' Item locks.
    ''' </summary>
    Private locks As List(Of DateLockInfo)

    ''' <summary>
    ''' Initializes a new instance of this class.
    ''' </summary>
    ''' <param name="fileSystemInfo">Corresponding file or folder in the file system.</param>
    ''' <param name="context">WebDAV Context.</param>
    ''' <param name="path">Encoded path relative to WebDAV root folder.</param>
    Protected Sub New(fileSystemInfo As FileSystemInfo, context As DavContext, path As String)
        Me.fileSystemInfo = fileSystemInfo
        Me.context = context
        Me.Path = path
    End Sub

    Public MustOverride Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync

    Public MustOverride Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync

    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync

    Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
        Dim propertyValues As List(Of PropertyValue) = Await GetPropertyValuesAsync()
        Dim snippet As PropertyName = props.FirstOrDefault(Function(s) s.Name = snippetProperty)
        If snippet.Name = snippetProperty AndAlso TypeOf Me Is DavFile Then
            propertyValues.Add(New PropertyValue(snippet, CType(Me, DavFile).Snippet))
        End If

        If Not allprop Then
            propertyValues = propertyValues.Where(Function(p) props.Contains(p.QualifiedName)).ToList()
        End If

        Return propertyValues
    End Function

    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync
        Dim propertyValues As IList(Of PropertyValue) = Await GetPropertyValuesAsync()
        Return propertyValues.Select(Function(p) p.QualifiedName)
    End Function

    Private Async Function GetPropertyValuesAsync() As Task(Of List(Of PropertyValue))
        If propertyValues Is Nothing Then
            propertyValues = Await fileSystemInfo.GetExtendedAttributeAsync(Of List(Of PropertyValue))(propertiesAttributeName)
        End If

        Return propertyValues
    End Function

    Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
        Await RequireHasTokenAsync()
        Dim propertyValues As List(Of PropertyValue) = Await GetPropertyValuesAsync()
        For Each propToSet As PropertyValue In setProps
            If propToSet.QualifiedName.Namespace = "urn:schemas-microsoft-com:" Then
                Select Case propToSet.QualifiedName.Name
                    Case "Win32CreationTime"
                        fileSystemInfo.CreationTimeUtc = DateTime.Parse(propToSet.Value,
                                                                       New System.Globalization.CultureInfo("en-US")).ToUniversalTime()
                    Case "Win32LastModifiedTime"
                        fileSystemInfo.LastWriteTimeUtc = DateTime.Parse(propToSet.Value,
                                                                        New System.Globalization.CultureInfo("en-US")).ToUniversalTime()
                    Case Else
                        context.Logger.LogDebug(String.Format("Unspecified case: DavHierarchyItem.UpdateProperties {0} from {1} namesapce",
                                                             propToSet.QualifiedName.Name, propToSet.QualifiedName.Namespace))
                End Select
            Else
                Dim existingProp As PropertyValue = propertyValues.FirstOrDefault(Function(p) p.QualifiedName = propToSet.QualifiedName)
                If existingProp IsNot Nothing Then
                    existingProp.Value = propToSet.Value
                Else
                    propertyValues.Add(propToSet)
                End If
            End If
        Next

        propertyValues.RemoveAll(Function(prop) delProps.Contains(prop.QualifiedName))
        Await fileSystemInfo.SetExtendedAttributeAsync(propertiesAttributeName, propertyValues)
        Await context.socketService.NotifyRefreshAsync(GetParentPath(Path))
    End Function

    Public Async Function GetFileAttributesAsync() As Task(Of FileAttributes) Implements IMsItemAsync.GetFileAttributesAsync
        If Name.StartsWith(".") Then
            Return fileSystemInfo.Attributes Or FileAttributes.Hidden
        End If

        Return fileSystemInfo.Attributes
    End Function

    Public Async Function SetFileAttributesAsync(value As FileAttributes) As Task Implements IMsItemAsync.SetFileAttributesAsync
        File.SetAttributes(fileSystemInfo.FullName, value)
    End Function

    Public Async Function GetActiveLocksAsync() As Task(Of IEnumerable(Of LockInfo)) Implements ILockAsync.GetActiveLocksAsync
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync()
        If locks Is Nothing Then
            Return New List(Of LockInfo)()
        End If

        Dim lockInfoList As IEnumerable(Of LockInfo) = locks.Select(Function(l) New LockInfo With {.IsDeep = l.IsDeep, .Level = l.Level, .Owner = l.ClientOwner, .LockRoot = l.LockRoot, .TimeOut = If(l.Expiration = DateTime.MaxValue, TimeSpan.MaxValue, l.Expiration - DateTime.UtcNow), .Token = l.LockToken}).ToList()
        Return lockInfoList
    End Function

    Public Async Function LockAsync(level As LockLevel, isDeep As Boolean, requestedTimeOut As TimeSpan?, owner As String) As Task(Of LockResult) Implements ILockAsync.LockAsync
        Await RequireUnlockedAsync(level = LockLevel.Shared)
        Dim token As String = Guid.NewGuid().ToString()
        Dim timeOut As TimeSpan = TimeSpan.FromMinutes(5)
        If requestedTimeOut.HasValue AndAlso requestedTimeOut < TimeSpan.MaxValue Then
            timeOut = requestedTimeOut.Value
        End If

        Dim lockInfo As DateLockInfo = New DateLockInfo With {.Expiration = DateTime.UtcNow + timeOut, .IsDeep = False, .Level = level, .LockRoot = Path, .LockToken = token, .ClientOwner = owner, .TimeOut = timeOut}
        Await SaveLockAsync(lockInfo)
        Await context.socketService.NotifyRefreshAsync(GetParentPath(Path))
        Return New LockResult(lockInfo.LockToken, lockInfo.TimeOut)
    End Function

    Public Async Function RefreshLockAsync(token As String, requestedTimeOut As TimeSpan?) As Task(Of RefreshLockResult) Implements ILockAsync.RefreshLockAsync
        If String.IsNullOrEmpty(token) Then
            Throw New DavException("Lock can not be found.", DavStatus.BAD_REQUEST)
        End If

        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync(getAllWithExpired:=True)
        Dim lockInfo As DateLockInfo = locks.SingleOrDefault(Function(x) x.LockToken = token)
        If lockInfo Is Nothing OrElse lockInfo.Expiration <= DateTime.UtcNow Then
            Throw New DavException("Lock can not be found.", DavStatus.CONFLICT)
        Else
            lockInfo.TimeOut = TimeSpan.FromMinutes(5)
            If requestedTimeOut.HasValue AndAlso requestedTimeOut < TimeSpan.MaxValue Then
                lockInfo.TimeOut = requestedTimeOut.Value
            End If

            lockInfo.Expiration = DateTime.UtcNow + lockInfo.TimeOut
            Await SaveLockAsync(lockInfo)
        End If

        Await context.socketService.NotifyRefreshAsync(GetParentPath(Path))
        Return New RefreshLockResult(lockInfo.Level, lockInfo.IsDeep, lockInfo.TimeOut, lockInfo.ClientOwner)
    End Function

    Public Async Function UnlockAsync(lockToken As String) As Task Implements ILockAsync.UnlockAsync
        If String.IsNullOrEmpty(lockToken) Then
            Throw New DavException("Lock can not be found.", DavStatus.BAD_REQUEST)
        End If

        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync(getAllWithExpired:=True)
        Dim lockInfo As DateLockInfo = locks.SingleOrDefault(Function(x) x.LockToken = lockToken)
        Await RemoveExpiredLocksAsync(lockToken)
        If lockInfo Is Nothing OrElse lockInfo.Expiration <= DateTime.UtcNow Then
            Throw New DavException("The lock could not be found.", DavStatus.CONFLICT)
        End If

        Await context.socketService.NotifyRefreshAsync(GetParentPath(Path))
    End Function

    Public Async Function RequireHasTokenAsync(Optional skipShared As Boolean = False) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync()
        If locks IsNot Nothing AndAlso locks.Any() Then
            Dim clientLockTokens As IList(Of String) = context.Request.ClientLockTokens
            If locks.All(Function(l) Not clientLockTokens.Contains(l.LockToken)) Then
                Throw New LockedException()
            End If
        End If
    End Function

    Public Async Function RequireUnlockedAsync(skipShared As Boolean) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync()
        If locks IsNot Nothing AndAlso locks.Any() Then
            If(skipShared AndAlso locks.Any(Function(l) l.Level = LockLevel.Exclusive)) OrElse (Not skipShared AndAlso locks.Any()) Then
                Throw New LockedException()
            End If
        End If
    End Function

    Private Async Function GetLocksAsync(Optional getAllWithExpired As Boolean = False) As Task(Of List(Of DateLockInfo))
        If locks Is Nothing Then
            locks = Await fileSystemInfo.GetExtendedAttributeAsync(Of List(Of DateLockInfo))(locksAttributeName)
            If locks IsNot Nothing Then
                locks.ForEach(Sub(l) __InlineAssignHelper(l.LockRoot, Path))
            End If
        End If

        If locks Is Nothing Then
            Return New List(Of DateLockInfo)()
        End If

        If getAllWithExpired Then
            Return locks
        Else
            Return locks.Where(Function(x) x.Expiration > DateTime.UtcNow).ToList()
        End If
    End Function

    Private Async Function SaveLockAsync(lockInfo As DateLockInfo) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync(getAllWithExpired:=True)
        'remove all expired locks
        'await RemoveExpiretLocksAsync();
        'you can call this method but it will be second file operation
        locks.RemoveAll(Function(x) x.Expiration <= DateTime.UtcNow)
        If locks.Any(Function(x) x.LockToken = lockInfo.LockToken) Then
            Dim existingLock As DateLockInfo = locks.[Single](Function(x) x.LockToken = lockInfo.LockToken)
            existingLock.TimeOut = lockInfo.TimeOut
            existingLock.Level = lockInfo.Level
            existingLock.IsDeep = lockInfo.IsDeep
            existingLock.LockRoot = lockInfo.LockRoot
            existingLock.Expiration = lockInfo.Expiration
            existingLock.ClientOwner = lockInfo.ClientOwner
        Else
            'add new item
            locks.Add(lockInfo)
        End If

        Await fileSystemInfo.SetExtendedAttributeAsync(locksAttributeName, locks)
    End Function

    Private Async Function RemoveExpiredLocksAsync(Optional unlockedToken As String = Nothing) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync(getAllWithExpired:=True)
        'remove expired and current lock
        locks.RemoveAll(Function(x) x.Expiration <= DateTime.UtcNow)
        If Not String.IsNullOrEmpty(unlockedToken) Then
            locks.RemoveAll(Function(x) x.LockToken = unlockedToken)
        End If

        Await fileSystemInfo.SetExtendedAttributeAsync(locksAttributeName, locks)
    End Function

    Protected Shared Function GetParentPath(path As String) As String
        Dim parentPath As String = $"/{path.Trim("/"c)}"
        Dim index As Integer = parentPath.LastIndexOf("/")
        parentPath = parentPath.Substring(0, index)
        Return parentPath
    End Function

    <Obsolete("Please refactor code that uses this function, it is a simple work-around to simulate inline assignment in VB!")>
    Private Shared Function __InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function
End Class
