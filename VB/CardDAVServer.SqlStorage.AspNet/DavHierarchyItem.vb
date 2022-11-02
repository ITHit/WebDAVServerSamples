Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal
Imports CardDAVServer.SqlStorage.AspNet.Acl
Imports ITHit.Server

''' <summary>
''' Base class for calendars (calendar folders), and calendar files (events and to-dos).
''' </summary>
Public MustInherit Class DavHierarchyItem
    Inherits Discovery
    Implements IHierarchyItem, ICurrentUserPrincipal

    Protected itemPath As String

    Protected displayName As String

    ''' <summary>
    ''' Gets item display name.
    ''' </summary>
    Public Overridable ReadOnly Property Name As String Implements IHierarchyItemBase.Name
        Get
            Return displayName
        End Get
    End Property

    ''' <summary>
    ''' Gets item path.
    ''' </summary>
    Public Overridable ReadOnly Property Path As String Implements IHierarchyItemBase.Path
        Get
            Return itemPath
        End Get
    End Property

    ''' <summary>
    ''' Gets item creation date. Must be in UTC.
    ''' </summary>
    Public Overridable ReadOnly Property Created As DateTime Implements IHierarchyItemBase.Created
        Get
            Return New DateTime(2000, 1, 1)
        End Get
    End Property

    ''' <summary>
    ''' Gets item modification date. Must be in UTC.
    ''' </summary>
    Public Overridable ReadOnly Property Modified As DateTime Implements IHierarchyItemBase.Modified
        Get
            Return New DateTime(2000, 1, 1)
        End Get
    End Property

    Public Sub New(context As DavContext)
        MyBase.New(context)
    End Sub

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
        Return New User(Context, Context.UserId)
    End Function

    Public Overridable Async Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItem.CopyToAsync
        Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
    End Function

    Public Overridable Async Function MoveToAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItem.MoveToAsync
        Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
    End Function

    Public MustOverride Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItem.DeleteAsync

    Public Overridable Async Function GetPropertiesAsync(names As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItem.GetPropertiesAsync
        Return New PropertyValue() {}
    End Function

    Public Overridable Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue),
                                                           delProps As IList(Of PropertyName),
                                                           multistatus As MultistatusException) As Task Implements IHierarchyItem.UpdatePropertiesAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItem.GetPropertyNamesAsync
        Return New PropertyName() {}
    End Function
End Class
