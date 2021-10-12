Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Extensibility
Imports ITHit.WebDAV.Server.Class2
Imports ITHit.WebDAV.Server.MicrosoftExtensions
Imports WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes
Imports ITHit.Server

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
    Public ReadOnly Property Name As String Implements IHierarchyItemBaseAsync.Name
        Get
            Return fileSystemInfo.Name
        End Get
    End Property

    ''' <summary>
    ''' Gets date when the item was created in UTC.
    ''' </summary>
    Public ReadOnly Property Created As DateTime Implements IHierarchyItemBaseAsync.Created
        Get
            Return fileSystemInfo.CreationTimeUtc
        End Get
    End Property

    ''' <summary>
    ''' Gets date when the item was last modified in UTC.
    ''' </summary>
    Public ReadOnly Property Modified As DateTime Implements IHierarchyItemBaseAsync.Modified
        Get
            Return fileSystemInfo.LastWriteTimeUtc
        End Get
    End Property

    ''' <summary>
    ''' Gets path of the item where each part between slashes is encoded.
    ''' </summary>
    Public Property Path As String Implements IHierarchyItemBaseAsync.Path

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

    ''' <summary>
    ''' Creates a copy of this item with a new name in the destination folder.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">Name of the destination item.</param>
    ''' <param name="deep">Indicates whether to copy entire subtree.</param>
    ''' <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using 
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    Public MustOverride Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync

    ''' <summary>
    ''' Moves this item to the destination folder under a new name.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">Name of the destination item.</param>
    ''' <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using 
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    Public MustOverride Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync

    ''' <summary>
    ''' Deletes this item.
    ''' </summary>
    ''' <param name="multistatus">If some items fail to delete but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync

    ''' <summary>
    ''' Retrieves user defined property values.
    ''' </summary>
    ''' <param name="names">Names of dead properties which values to retrieve.</param>
    ''' <param name="allprop">Whether all properties shall be retrieved.</param>
    ''' <returns>Property values.</returns>
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

    ''' <summary>
    ''' Retrieves names of all user defined properties.
    ''' </summary>
    ''' <returns>Property names.</returns>
    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync
        Dim propertyValues As IList(Of PropertyValue) = Await GetPropertyValuesAsync()
        Return propertyValues.Select(Function(p) p.QualifiedName)
    End Function

    ''' <summary>
    ''' Retrieves list of user defined propeties for this item.
    ''' </summary>
    ''' <returns>List of user defined properties.</returns>
    Private Async Function GetPropertyValuesAsync() As Task(Of List(Of PropertyValue))
        Dim properties As List(Of PropertyValue) = New List(Of PropertyValue)()
        If Await fileSystemInfo.HasExtendedAttributeAsync(propertiesAttributeName) Then
            properties = Await fileSystemInfo.GetExtendedAttributeAsync(Of List(Of PropertyValue))(propertiesAttributeName)
        End If

        Return properties
    End Function

    ''' <summary>
    ''' Saves property values to extended attribute.
    ''' </summary>
    ''' <param name="setProps">Properties to be set.</param>
    ''' <param name="delProps">Properties to be deleted.</param>
    ''' <param name="multistatus">Information about properties that failed to create, update or delate.</param>
    Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
        Await RequireHasTokenAsync()
        Dim propertyValues As List(Of PropertyValue) = Await GetPropertyValuesAsync()
        For Each propToSet As PropertyValue In setProps
            ' Microsoft Mini-redirector may update file creation date, modification date and access time passing properties:
            ' <Win32CreationTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:15:34 GMT</Win32CreationTime>
            ' <Win32LastModifiedTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastModifiedTime>
            ' <Win32LastAccessTime xmlns="urn:schemas-microsoft-com:">Thu, 28 Mar 2013 20:36:24 GMT</Win32LastAccessTime>
            ' In this case update creation and modified date in your storage or do not save this properties at all, otherwise 
            ' Windows Explorer will display creation and modification date from this props and it will differ from the values 
            ' in the Created and Modified fields in your storage 
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
        Await context.socketService.NotifyUpdatedAsync(Path)
    End Function

    ''' <summary>
    ''' Returns Windows file attributes (readonly, hidden etc.) for this file/folder.
    ''' </summary>
    ''' <returns>Windows file attributes.</returns>
    Public Async Function GetFileAttributesAsync() As Task(Of FileAttributes) Implements IMsItemAsync.GetFileAttributesAsync
        If Name.StartsWith(".") Then
            Return fileSystemInfo.Attributes Or FileAttributes.Hidden
        End If

        Return fileSystemInfo.Attributes
    End Function

    ''' <summary>
    ''' Sets Windows file attributes (readonly, hidden etc.) on this item.
    ''' </summary>
    ''' <param name="value">File attributes.</param>
    Public Async Function SetFileAttributesAsync(value As FileAttributes) As Task Implements IMsItemAsync.SetFileAttributesAsync
        File.SetAttributes(fileSystemInfo.FullName, value)
    End Function

    ''' <summary>
    ''' Retrieves non expired locks for this item.
    ''' </summary>
    ''' <returns>Enumerable with information about locks.</returns>
    Public Async Function GetActiveLocksAsync() As Task(Of IEnumerable(Of LockInfo)) Implements ILockAsync.GetActiveLocksAsync
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync()
        If locks Is Nothing Then
            Return New List(Of LockInfo)()
        End If

        Dim lockInfoList As IEnumerable(Of LockInfo) = locks.Select(Function(l) New LockInfo With {.IsDeep = l.IsDeep,
                                                                                             .Level = l.Level,
                                                                                             .Owner = l.ClientOwner,
                                                                                             .LockRoot = l.LockRoot,
                                                                                             .TimeOut = If(l.Expiration = DateTime.MaxValue, TimeSpan.MaxValue, l.Expiration - DateTime.UtcNow),
                                                                                             .Token = l.LockToken
                                                                                             }).ToList()
        Return lockInfoList
    End Function

    ''' <summary>
    ''' Locks this item.
    ''' </summary>
    ''' <param name="level">Whether lock is share or exclusive.</param>
    ''' <param name="isDeep">Whether lock is deep.</param>
    ''' <param name="requestedTimeOut">Lock timeout which was requested by client.
    ''' Server may ignore this parameter and set any timeout.</param>
    ''' <param name="owner">Owner of the lock as specified by client.</param> 
    ''' <returns>
    ''' Instance of <see cref="LockResult"/>  with information about the lock.
    ''' </returns>
    Public Async Function LockAsync(level As LockLevel, isDeep As Boolean, requestedTimeOut As TimeSpan?, owner As String) As Task(Of LockResult) Implements ILockAsync.LockAsync
        Await RequireUnlockedAsync(level = LockLevel.Shared)
        Dim token As String = Guid.NewGuid().ToString()
        ' If timeout is absent or infinit timeout requested,
        ' grant 5 minute lock.
        Dim timeOut As TimeSpan = TimeSpan.FromMinutes(5)
        If requestedTimeOut.HasValue AndAlso requestedTimeOut < TimeSpan.MaxValue Then
            timeOut = requestedTimeOut.Value
        End If

        Dim lockInfo As DateLockInfo = New DateLockInfo With {.Expiration = DateTime.UtcNow + timeOut,
                                                        .IsDeep = False,
                                                        .Level = level,
                                                        .LockRoot = Path,
                                                        .LockToken = token,
                                                        .ClientOwner = owner,
                                                        .TimeOut = timeOut
                                                        }
        Await SaveLockAsync(lockInfo)
        Await context.socketService.NotifyLockedAsync(Path)
        Return New LockResult(lockInfo.LockToken, lockInfo.TimeOut)
    End Function

    ''' <summary>
    ''' Updates lock timeout information on this item.
    ''' </summary>
    ''' <param name="token">Lock token.</param>
    ''' <param name="requestedTimeOut">Lock timeout which was requested by client.
    ''' Server may ignore this parameter and set any timeout.</param>
    ''' <returns>
    ''' Instance of <see cref="LockResult"/>  with information about the lock.
    ''' </returns>
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

        Await context.socketService.NotifyLockedAsync(Path)
        Return New RefreshLockResult(lockInfo.Level, lockInfo.IsDeep, lockInfo.TimeOut, lockInfo.ClientOwner)
    End Function

    ''' <summary>
    ''' Removes lock with the specified token from this item.
    ''' </summary>
    ''' <param name="lockToken">Lock with this token should be removed from the item.</param>
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

        Await context.socketService.NotifyUnLockedAsync(Path)
    End Function

    ''' <summary>
    ''' Check that if the item is locked then client has submitted correct lock token.
    ''' </summary>
    Public Async Function RequireHasTokenAsync(Optional skipShared As Boolean = False) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync()
        If locks IsNot Nothing AndAlso locks.Any() Then
            Dim clientLockTokens As IList(Of String) = context.Request.GetClientLockTokens()
            If locks.All(Function(l) Not clientLockTokens.Contains(l.LockToken)) Then
                Throw New LockedException()
            End If
        End If
    End Function

    ''' <summary>
    ''' Ensure that there are no active locks on the item.
    ''' </summary>
    ''' <param name="skipShared">Whether shared locks shall be checked.</param>
    Public Async Function RequireUnlockedAsync(skipShared As Boolean) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync()
        If locks IsNot Nothing AndAlso locks.Any() Then
            If(skipShared AndAlso locks.Any(Function(l) l.Level = LockLevel.Exclusive)) OrElse (Not skipShared AndAlso locks.Any()) Then
                Throw New LockedException()
            End If
        End If
    End Function

    ''' <summary>
    ''' Retrieves non-expired locks acquired on this item.
    ''' </summary>
    ''' <param name="getAllWithExpired">Indicate needed return expired locks.</param>
    ''' <returns>List of locks with their expiration dates.</returns>
    Private Async Function GetLocksAsync(Optional getAllWithExpired As Boolean = False) As Task(Of List(Of DateLockInfo))
        Dim locks As List(Of DateLockInfo) = New List(Of DateLockInfo)()
        If Await fileSystemInfo.HasExtendedAttributeAsync(locksAttributeName) Then
            locks = Await fileSystemInfo.GetExtendedAttributeAsync(Of List(Of DateLockInfo))(locksAttributeName)
            If locks IsNot Nothing Then
                locks.ForEach(Sub(l) __InlineAssignHelper(l.LockRoot, Path))
            End If
        End If

        If getAllWithExpired Then
            Return locks
        Else
            Return locks.Where(Function(x) x.Expiration > DateTime.UtcNow).ToList()
        End If
    End Function

    ''' <summary>
    ''' Saves lock acquired on this file/folder.
    ''' </summary>
    Private Async Function SaveLockAsync(lockInfo As DateLockInfo) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync(getAllWithExpired:=True)
        'remove all expired locks
        'await RemoveExpiretLocksAsync();
        'you can call this method but it will be second file operation
        locks.RemoveAll(Function(x) x.Expiration <= DateTime.UtcNow)
        If locks.Any(Function(x) x.LockToken = lockInfo.LockToken) Then
            'update value
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

    Private Async Function RemoveExpiredLocksAsync(unlockedToken As String) As Task
        Dim locks As List(Of DateLockInfo) = Await GetLocksAsync(getAllWithExpired:=True)
        'remove expired and current lock
        locks.RemoveAll(Function(x) x.Expiration <= DateTime.UtcNow)
        'remove from token
        If Not String.IsNullOrEmpty(unlockedToken) Then
            locks.RemoveAll(Function(x) x.LockToken = unlockedToken)
        End If

        Await fileSystemInfo.SetExtendedAttributeAsync(locksAttributeName, locks)
    End Function

    ''' <summary>
    ''' Gets element's parent path. 
    ''' </summary>
    ''' <param name="path">Element's path.</param>
    ''' <returns>Path to parent element.</returns>
    Protected Shared Function GetParentPath(path As String) As String
        Dim parentPath As String = String.Format("/{0}", path.Trim("/"c))
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
