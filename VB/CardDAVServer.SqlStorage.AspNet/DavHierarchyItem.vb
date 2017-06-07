Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports CardDAVServer.SqlStorage.AspNet.Acl

''' <summary>
''' Base class for calendars (calendar folders), and calendar files (events and to-dos).
''' </summary>
Public MustInherit Class DavHierarchyItem
    Inherits Discovery
    Implements IHierarchyItemAsync, ICurrentUserPrincipalAsync

    Protected itemPath As String

    Protected displayName As String

    ''' <summary>
    ''' Gets item display name.
    ''' </summary>
    Public Overridable ReadOnly Property Name As String Implements IHierarchyItemAsync.Name
        Get
            Return displayName
        End Get
    End Property

    ''' <summary>
    ''' Gets item path.
    ''' </summary>
    Public Overridable ReadOnly Property Path As String Implements IHierarchyItemAsync.Path
        Get
            Return itemPath
        End Get
    End Property

    ''' <summary>
    ''' Gets item creation date. Must be in UTC.
    ''' </summary>
    Public Overridable ReadOnly Property Created As DateTime Implements IHierarchyItemAsync.Created
        Get
            Return New DateTime(2000, 1, 1)
        End Get
    End Property

    ''' <summary>
    ''' Gets item modification date. Must be in UTC.
    ''' </summary>
    Public Overridable ReadOnly Property Modified As DateTime Implements IHierarchyItemAsync.Modified
        Get
            Return New DateTime(2000, 1, 1)
        End Get
    End Property

    Public Sub New(context As DavContext)
        MyBase.New(context)
    End Sub

    Public Async Function GetCurrentUserPrincipalAsync() As Task(Of IPrincipalAsync) Implements ICurrentUserPrincipalAsync.GetCurrentUserPrincipalAsync
        Return New User(Context, Context.UserId)
    End Function

    Public Overridable Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
    End Function

    Public Overridable Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
    End Function

    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync

    Public Overridable Async Function GetPropertiesAsync(names As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
        Return New PropertyValue() {}
    End Function

    Public Overridable Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue),
                                                           delProps As IList(Of PropertyName),
                                                           multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync
        Return New PropertyName() {}
    End Function
End Class
