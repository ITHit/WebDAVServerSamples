Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Paging

Namespace Acl

    ''' <summary>
    ''' Logical folder right under webdav root with name 'acl' which
    ''' contains folders 'users' and 'groups'.
    ''' </summary>
    Public Class AclFolder
        Inherits LogicalFolder

        ''' <summary>
        ''' Path to current logical folder which contains users and groups.
        ''' </summary>
        Public Shared PREFIX As String = "acl"

        ''' <summary>
        ''' Gets path to current logical folder which contains users and groups.
        ''' </summary>
        Private Shared ReadOnly Property PATH As String
            Get
                Return PREFIX & "/"
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the AclFolder class.
        ''' </summary>
        ''' <param name="context">Instace of <see cref="DavContext"/> .</param>
        Public Sub New(context As DavContext)
            MyBase.New(context, "acl", PATH)
        End Sub

        ''' <summary>
        ''' Retrieves children of /acl folder.
        ''' We have here 'user' and 'group' folder for holding users and groups respectively.
        ''' </summary>
        ''' <param name="propNames">Property names to be fetched lated.</param>
        ''' <returns>Children of this folder.</returns>
        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults)
            Dim children As IList(Of IHierarchyItem) = New List(Of IHierarchyItem)()
            children.Add(New UserFolder(Context))
            children.Add(New GroupFolder(Context))
            Return New PageResults(children, Nothing)
        End Function
    End Class
End Namespace
