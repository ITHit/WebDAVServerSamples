Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal
Imports ITHit.WebDAV.Server.Extensibility
Imports CalDAVServer.FileSystemStorage.AspNet.Acl
Imports CalDAVServer.FileSystemStorage.AspNet.ExtendedAttributes
Imports ITHit.Server

''' <summary>
''' Base class for WebDAV items (folders, files, etc).
''' </summary>
Public MustInherit Class DavHierarchyItem
    Inherits Discovery
    Implements IHierarchyItem, ICurrentUserPrincipal

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
    Public ReadOnly Property Name As String Implements IHierarchyItemBase.Name
        Get
            Return fileSystemInfo.Name
        End Get
    End Property

    ''' <summary>
    ''' Gets date when the item was created in UTC.
    ''' </summary>
    Public ReadOnly Property Created As DateTime Implements IHierarchyItemBase.Created
        Get
            Return fileSystemInfo.CreationTimeUtc
        End Get
    End Property

    ''' <summary>
    ''' Gets date when the item was last modified in UTC.
    ''' </summary>
    Public ReadOnly Property Modified As DateTime Implements IHierarchyItemBase.Modified
        Get
            Return fileSystemInfo.LastWriteTimeUtc
        End Get
    End Property

    ''' <summary>
    ''' Gets path of the item where each part between slashes is encoded.
    ''' </summary>
    Public Property Path As String Implements IHierarchyItemBase.Path

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
        MyBase.New(context)
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
    Public MustOverride Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItem.CopyToAsync

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
    ''' <param name="recursionDepth">Recursion depth.</param>
    Public MustOverride Function CopyToInternalAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException, recursionDepth As Integer) As Task

    ''' <summary>
    ''' Moves this item to the destination folder under a new name.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">Name of the destination item.</param>
    ''' <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using 
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    Public MustOverride Function MoveToAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItem.MoveToAsync

    ''' <summary>
    ''' Moves this item to the destination folder under a new name.
    ''' </summary>
    ''' <param name="destFolder">Destination folder.</param>
    ''' <param name="destName">Name of the destination item.</param>
    ''' <param name="multistatus">If some items fail to copy but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using 
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    ''' <param name="recursionDepth">Recursion depth.</param>
    Public MustOverride Function MoveToInternalAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException, recursionDepth As Integer) As Task

    ''' <summary>
    ''' Deletes this item.
    ''' </summary>
    ''' <param name="multistatus">If some items fail to delete but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItem.DeleteAsync

    ''' <summary>
    ''' Deletes this item.
    ''' </summary>
    ''' <param name="multistatus">If some items fail to delete but operation in whole shall be continued, add
    ''' information about the error into <paramref name="multistatus"/>  using
    ''' <see cref="MultistatusException.AddInnerException(string, ITHit.WebDAV.Server.DavException)"/> .
    ''' </param>
    ''' <param name="recursionDepth">Recursion depth.</param>
    Public MustOverride Function DeleteInternalAsync(multistatus As MultistatusException, recursionDepth As Integer) As Task

    ''' <summary>
    ''' Retrieves user defined property values.
    ''' </summary>
    ''' <param name="names">Names of dead properties which values to retrieve.</param>
    ''' <param name="allprop">Whether all properties shall be retrieved.</param>
    ''' <returns>Property values.</returns>
    Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItem.GetPropertiesAsync
        Dim propertyValues As List(Of PropertyValue) = Await GetPropertyValuesAsync()
        If Not allprop Then
            propertyValues = propertyValues.Where(Function(p) props.Contains(p.QualifiedName)).ToList()
        End If

        Return propertyValues
    End Function

    ''' <summary>
    ''' Retrieves names of all user defined properties.
    ''' </summary>
    ''' <returns>Property names.</returns>
    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItem.GetPropertyNamesAsync
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
    Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItem.UpdatePropertiesAsync
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
    End Function

    ''' <summary>
    ''' Returns instance of <see cref="IPrincipal"/>  which represents current user.
    ''' </summary>
    ''' <returns>Current user.</returns>
    ''' <remarks>
    ''' This method is usually called by the Engine when CalDAV/CardDAV client 
    ''' is trying to discover current user URL.
    ''' </remarks>
    Public Async Function GetCurrentUserPrincipalAsync() As Task(Of IPrincipal) Implements ICurrentUserPrincipal.GetCurrentUserPrincipalAsync
        ' Typically there is no need to load all user properties here, only current 
        ' user ID (or name) is required to form the user URL: [DAVLocation]/acl/users/[UserID]
        Return AclFactory.GetPrincipalFromSid(context.WindowsIdentity.User.Value, context)
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

    ''' <summary>
    ''' Returns WebSocket client ID.
    ''' </summary>
    ''' <returns>Client ID.</returns>
    Protected Function GetWebSocketID() As String
        Return If(context.Request.Headers.ContainsKey("InstanceId"), context.Request.Headers("InstanceId"), String.Empty)
    End Function
End Class
