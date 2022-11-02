Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Paging
Imports ITHit.Server

''' <summary>
''' Base class for logical folders which are not present in file system, like '/acl/,
''' '/acl/groups/'
''' </summary>
Public MustInherit Class LogicalFolder
    Inherits Discovery
    Implements IItemCollection

    Public Property Context As DavContext

    Public Property Name As String Implements IHierarchyItemBase.Name

    Public Property Path As String Implements IHierarchyItemBase.Path

    Protected Sub New(context As DavContext, name As String, path As String)
        MyBase.New(context)
        Me.Context = context
        Me.Name = name
        Me.Path = path
    End Sub

    Public ReadOnly Property Created As DateTime Implements IHierarchyItemBase.Created
        Get
            Return DateTime.UtcNow
        End Get
    End Property

    Public ReadOnly Property Modified As DateTime Implements IHierarchyItemBase.Modified
        Get
            Return DateTime.UtcNow
        End Get
    End Property

    Public Async Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItem.CopyToAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function MoveToAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItem.MoveToAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItem.DeleteAsync
        Throw New NotImplementedException()
    End Function

    Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItem.GetPropertiesAsync
        Return New PropertyValue(-1) {}
    End Function

    Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItem.GetPropertyNamesAsync
        Return New PropertyName(-1) {}
    End Function

    Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItem.UpdatePropertiesAsync
        Throw New NotImplementedException()
    End Function

    Public MustOverride Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollection.GetChildrenAsync
End Class
