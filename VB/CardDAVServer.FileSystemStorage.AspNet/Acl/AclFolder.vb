Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

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

        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync))
            Dim children As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
            children.Add(New UserFolder(Context))
            children.Add(New GroupFolder(Context))
            Return children
        End Function
    End Class
End Namespace
