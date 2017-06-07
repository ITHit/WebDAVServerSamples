Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Security.AccessControl
Imports System.Security.Principal
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports CalDAVServer.FileSystemStorage.AspNet.Acl
Imports CalDAVServer.FileSystemStorage.AspNet.ExtendedAttributes

''' <summary>
''' Base class for WebDAV items (folders, files, etc).
''' </summary>
Public MustInherit Class DavHierarchyItem
    Inherits Discovery
    Implements IHierarchyItemAsync, ICurrentUserPrincipalAsync

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

    Public MustOverride Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync

    Public MustOverride Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync

    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync

    Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
        Dim propertyValues As List(Of PropertyValue) = Await GetPropertyValuesAsync()
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
    End Function

    Public Async Function GetCurrentUserPrincipalAsync() As Task(Of IPrincipalAsync) Implements ICurrentUserPrincipalAsync.GetCurrentUserPrincipalAsync
        Return AclFactory.GetPrincipalFromSid(context.WindowsIdentity.User.Value, context)
    End Function

    Protected Shared Function GetParentPath(path As String) As String
        Dim parentPath As String = $"/{path.Trim("/"c)}"
        Dim index As Integer = parentPath.LastIndexOf("/")
        parentPath = parentPath.Substring(0, index)
        Return parentPath
    End Function
End Class
