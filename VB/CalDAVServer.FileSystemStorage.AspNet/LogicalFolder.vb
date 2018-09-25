Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Paging

''' <summary>
''' Base class for logical folders which are not present in file system, like '/acl/,
''' '/acl/groups/'
''' </summary>
Public MustInherit Class LogicalFolder
    Inherits Discovery
    Implements IItemCollectionAsync

    Public Property Context As DavContext

    Public Property Name As String Implements IHierarchyItemAsync.Name

    Public Property Path As String Implements IHierarchyItemAsync.Path

    Protected Sub New(context As DavContext, name As String, path As String)
        MyBase.New(context)
        Me.Context = context
        Me.Name = name
        Me.Path = path
    End Sub

    Public ReadOnly Property Created As DateTime Implements IHierarchyItemAsync.Created
        Get
            Return DateTime.UtcNow
        End Get
    End Property

    Public ReadOnly Property Modified As DateTime Implements IHierarchyItemAsync.Modified
        Get
            Return DateTime.UtcNow
        End Get
    End Property

    Public Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
        Return New PropertyValue(-1) {}
    End Function

    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync
        Return New PropertyName(-1) {}
    End Function

    Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
        Throw New NotImplementedException()
    End Function

    Public MustOverride Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollectionAsync.GetChildrenAsync
End Class
