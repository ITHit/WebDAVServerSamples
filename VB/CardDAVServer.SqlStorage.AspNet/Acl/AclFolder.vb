Imports System
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Paging

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

        ''' <summary>
        ''' Retrieves children of this folder.
        ''' </summary>
        ''' <param name="propNames">Properties requested by client application for each child.</param>
        ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        ''' <param name="nResults">The number of items to return.</param>
        ''' <param name="orderProps">List of order properties requested by the client.</param>
        ''' <returns>Children of this folder.</returns>
        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults)
            ' In this samle we list users folder only. Groups and groups folder is not implemented.
            Return New PageResults({New UsersFolder(Context)}, Nothing)
        End Function
    End Class
End Namespace
