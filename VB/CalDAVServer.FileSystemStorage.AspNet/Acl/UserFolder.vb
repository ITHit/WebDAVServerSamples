Imports System.Collections.Generic
Imports System.DirectoryServices.AccountManagement
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    ''' <summary>
    ''' Logical folder which contains users.
    ''' It has path '/acl/users'
    ''' </summary>
    Public Class UserFolder
        Inherits LogicalFolder
        Implements IPrincipalFolderAsync

        ''' <summary>
        ''' Path to folder which contains users.
        ''' </summary>
        Public Shared PREFIX As String = AclFolder.PREFIX & "/users"

        ''' <summary>
        ''' Gets path of folder which contains users.
        ''' </summary>
        Public Shared ReadOnly Property PATH As String
            Get
                Return PREFIX & "/"
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the <see cref="UserFolder"/>  class.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        Public Sub New(context As DavContext)
            MyBase.New(context, "users", PATH)
        End Sub

        ''' <summary>
        ''' Retrieves users.
        ''' </summary>
        ''' <param name="properties">List of properties which will be retrieved by the engine later.</param>
        ''' <returns>Enumerable with users.</returns>
        Public Overrides Async Function GetChildrenAsync(properties As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
            Return Context.PrincipalOperation(Of IEnumerable(Of IHierarchyItemAsync))(AddressOf getUsers)
        End Function

        ''' <summary>
        ''' Retrieves all users in computer/domain.
        ''' </summary>
        ''' <returns>Enumerable with users.</returns>
        Private Function getUsers() As IEnumerable(Of IHierarchyItemAsync)
            Dim insUserPrincipal As UserPrincipal = New UserPrincipal(Context.GetPrincipalContext())
            insUserPrincipal.Name = "*"
            Dim insPrincipalSearcher As PrincipalSearcher = New PrincipalSearcher(insUserPrincipal)
            Return insPrincipalSearcher.FindAll().Select(Function(u) New User(CType(u, UserPrincipal), Context)).Cast(Of IHierarchyItemAsync)().ToList()
        End Function

        ''' <summary>
        ''' We don't support creating folders inside this folder.
        ''' </summary>        
        Public Async Function CreateFolderAsync(name As String) As Task(Of IPrincipalFolderAsync) Implements IPrincipalFolderAsync.CreateFolderAsync
            Throw New DavException("Creating folders is not implemented", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Creates user.
        ''' </summary>
        ''' <param name="name">User name.</param>
        ''' <returns>Newly created user.</returns>
        Public Async Function CreatePrincipalAsync(name As String) As Task(Of IPrincipalAsync) Implements IPrincipalFolderAsync.CreatePrincipalAsync
            If Not PrincipalBase.IsValidUserName(name) Then
                Throw New DavException("User name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim userPrincipal As UserPrincipal = New UserPrincipal(Context.GetPrincipalContext())
            userPrincipal.Name = name
            userPrincipal.UserPrincipalName = name
            userPrincipal.Enabled = True
            userPrincipal.ExpirePasswordNow()
            Context.PrincipalOperation(AddressOf userPrincipal.Save)
            Return New User(userPrincipal, Context)
        End Function

        ''' <summary>
        ''' Finds users whose properties have certain values.
        ''' </summary>
        ''' <param name="propValues">Properties and values to look for.</param>
        ''' <param name="props">Properties that will be requested by the engine from the returned users.</param>
        ''' <returns>Enumerable with users whose properties match.</returns>
        Public Async Function FindPrincipalsByPropertyValuesAsync(propValues As IList(Of PropertyValue),
                                                                 props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.FindPrincipalsByPropertyValuesAsync
            Dim user As UserPrincipal = New UserPrincipal(Context.GetPrincipalContext())
            user.Name = "*"
            For Each v As PropertyValue In propValues
                If v.QualifiedName = PropertyName.DISPLAYNAME Then
                    user.Name = "*" & v.Value & "*"
                End If
            Next

            Dim searcher As PrincipalSearcher = New PrincipalSearcher(user)
            Return searcher.FindAll().Select(Function(u) New User(CType(u, UserPrincipal), Context)).Cast(Of IPrincipalAsync)()
        End Function

        ''' <summary>
        ''' Returns list of properties which can be used in <see cref="FindPrincipalsByPropertyValuesAsync"/> .
        ''' </summary>
        ''' <returns></returns>
        Public Async Function GetPrincipalSearcheablePropertiesAsync() As Task(Of IEnumerable(Of PropertyDescription)) Implements IPrincipalFolderAsync.GetPrincipalSearcheablePropertiesAsync
            Return {New PropertyDescription With {.Name = PropertyName.DISPLAYNAME,                                 
                                            .Description = "Principal name",
                                            .Lang = "en"}}
        End Function

        ''' <summary>
        ''' Returns <see cref="IPrincipalAsync"/>  for the current user.
        ''' </summary>
        ''' <param name="props">Properties that will be asked later from the user returned.</param>
        ''' <returns>Enumerable with users.</returns>
        Public Async Function GetMatchingPrincipalsAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.GetMatchingPrincipalsAsync
            Return {User.FromName(Context.WindowsIdentity.Name, Context)}
        End Function
    End Class
End Namespace
