Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace Acl

    ''' <summary>
    ''' Logical folder with name 'acl' which contains folders 'users' and 'groups'.
    ''' Instances of this class correspond to the following path: [DAVLocation]/acl/
    ''' </summary>
    Public Class AclFolder
        Inherits LogicalFolder

        ''' <summary>
        ''' This folder name.
        ''' </summary>
        Private Shared ReadOnly aclFolderName As String = "acl"

        ''' <summary>
        ''' Path to this folder.
        ''' </summary>
        Public Shared ReadOnly AclFolderPath As String = String.Format("{0}{1}/", DavLocationFolder.DavLocationFolderPath, aclFolderName)

        ''' <summary>
        ''' Initializes a new instance of the AclFolder class.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <param name="name">Folder name.</param>
        ''' <param name="path">Encoded path relative to WebDAV root.</param>
        Public Sub New(context As DavContext)
            MyBase.New(context, AclFolderPath)
        End Sub

        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync))
            Return {New UsersFolder(Context)}
        End Function
    End Class
End Namespace
