Imports System
Imports System.Collections.Generic
Imports System.Web
Imports System.Web.Security
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.Paging
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal

Namespace Acl

    ''' <summary>
    ''' Logical folder which contains users.
    ''' Instances of this class correspond to the following path: [DAVLocation]/acl/users/.
    ''' </summary>
    Public Class UsersFolder
        Inherits LogicalFolder
        Implements IPrincipalFolder

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

        ''' <summary>
        ''' Retrieves users.
        ''' </summary>
        ''' <param name="propNames">Properties requested by client application for each child.</param>
        ''' <param name="offset">The number of children to skip before returning the remaining items. Start listing from from next item.</param>
        ''' <param name="nResults">The number of items to return.</param>
        ''' <param name="orderProps">List of order properties requested by the client.</param>
        ''' <returns>Children of this folder - list of user principals.</returns>
        Public Overrides Async Function GetChildrenAsync(propNames As IList(Of PropertyName), offset As Long?, nResults As Long?, orderProps As IList(Of OrderProperty)) As Task(Of PageResults) Implements IItemCollection.GetChildrenAsync
            ' Here you will list users from OWIN Identity or from membership provider, 
            ' you can replace it with your own users source.
            ' In this implementation we return only one user - current user, for demo purposes.
            ' We also do not populate user e-mail to avoid any queries to back-end storage.
            Dim children As IList(Of IHierarchyItem) = New List(Of IHierarchyItem)()
            children.Add(New User(Context, Context.UserId, Context.Identity.Name, Nothing, New DateTime(2000, 1, 1), New DateTime(2000, 1, 1)))
            Return New PageResults(children, Nothing)
        End Function

        ''' <summary>
        ''' We don't support creating folders inside this folder.
        ''' </summary>        
        Public Async Function CreateFolderAsync(name As String) As Task(Of IPrincipalFolder) Implements IPrincipalFolder.CreateFolderAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Creates user.
        ''' </summary>
        ''' <param name="name">User name.</param>
        ''' <returns>Newly created user.</returns>
        Public Async Function CreatePrincipalAsync(name As String) As Task(Of IPrincipal) Implements IPrincipalFolder.CreatePrincipalAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Finds users whose properties have certain values.
        ''' </summary>
        ''' <param name="propValues">Properties and values to look for.</param>
        ''' <param name="props">Properties that will be requested by the engine from the returned users.</param>
        ''' <returns>Enumerable with users whose properties match.</returns>
        Public Async Function FindPrincipalsByPropertyValuesAsync(propValues As IList(Of PropertyValue),
                                                                 props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipal)) Implements IPrincipalFolder.FindPrincipalsByPropertyValuesAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Returns list of properties which can be used in <see cref="FindPrincipalsByPropertyValues"/> .
        ''' </summary>
        ''' <returns></returns>
        Public Async Function GetPrincipalSearcheablePropertiesAsync() As Task(Of IEnumerable(Of PropertyDescription)) Implements IPrincipalFolder.GetPrincipalSearcheablePropertiesAsync
            Return New PropertyDescription(-1) {}
        End Function

        ''' <summary>
        ''' Returns <see cref="IPrincipal"/>  for the current user.
        ''' </summary>
        ''' <param name="props">Properties that will be asked later from the user returned.</param>
        ''' <returns>Enumerable with users.</returns>
        Public Async Function GetMatchingPrincipalsAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipal)) Implements IPrincipalFolder.GetMatchingPrincipalsAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function
    End Class
End Namespace
