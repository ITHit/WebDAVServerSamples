Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Paging

''' <summary>
''' Base class for logical folders which are not present in your 
''' back-end storage (datatbase, file system, etc), like [DavLocation], '[DavLocation]/acl/, '[DavLocation]/acl/users/'
''' </summary>
Public Class LogicalFolder
    Inherits DavHierarchyItem
    Implements IItemCollection

    Private children As IEnumerable(Of IHierarchyItem)

    ''' <summary>
    ''' Creates instance of <see cref="LogicalFolder"/>  class.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="DavContext"/></param>
    ''' <param name="path">Encoded path relative to WebDAV root.</param>
    ''' <param name="children">List of child items that will be returned when enumerating this folder children.</param>
    Public Sub New(context As DavContext, path As String, Optional children As IEnumerable(Of IHierarchyItem) = Nothing)
        MyBase.New(context)
        Me.Context = context
        Me.itemPath = path
        Me.children = If(children, New IHierarchyItem(-1) {})
        path = path.TrimEnd("/"c)
        Dim encodedName As String = path.Substring(path.LastIndexOf("/"c) + 1)
        Me.displayName = EncodeUtil.DecodeUrlPart(encodedName)
    End Sub

    Public Overrides Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItem.DeleteAsync
        Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
    End Function

    Public Overridable Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollection.GetChildrenAsync
        Return New PageResults(children, Nothing)
    End Function
End Class
