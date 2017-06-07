Imports System
Imports System.Collections.Generic
Imports System.Web
Imports System.Web.Security
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    ''' <summary>
    ''' Logical folder which contains users.
    ''' Instances of this class correspond to the following path: [DAVLocation]/acl/users/.
    ''' </summary>
    Public Class UsersFolder
        Inherits LogicalFolder
        Implements IPrincipalFolderAsync

        ''' <summary>
        ''' This folder name.
        ''' </summary>
        Private Shared ReadOnly usersFolderName As String = "users"

        ''' <summary>
        ''' Path to this folder.
        ''' </summary>
        Public Shared ReadOnly UsersFolderPath As String = String.Format("{0}{1}/", AclFolder.AclFolderPath, usersFolderName)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="UsersFolder"/>  class.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        Public Sub New(context As DavContext)
            MyBase.New(context, UsersFolderPath)
        End Sub

        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
            Dim children As IList(Of IHierarchyItemAsync) = New List(Of IHierarchyItemAsync)()
            children.Add(New User(Context, Context.UserId, Context.Identity.Name, Nothing, New DateTime(2000, 1, 1), New DateTime(2000, 1, 1)))
            Return children
        End Function

        Public Async Function CreateFolderAsync(name As String) As Task(Of IPrincipalFolderAsync) Implements IPrincipalFolderAsync.CreateFolderAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function CreatePrincipalAsync(name As String) As Task(Of IPrincipalAsync) Implements IPrincipalFolderAsync.CreatePrincipalAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function FindPrincipalsByPropertyValuesAsync(propValues As IList(Of PropertyValue),
                                                                 props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.FindPrincipalsByPropertyValuesAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function GetPrincipalSearcheablePropertiesAsync() As Task(Of IEnumerable(Of PropertyDescription)) Implements IPrincipalFolderAsync.GetPrincipalSearcheablePropertiesAsync
            Return New PropertyDescription(-1) {}
        End Function

        Public Async Function GetMatchingPrincipalsAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.GetMatchingPrincipalsAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function
    End Class
End Namespace
