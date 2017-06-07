Imports System.Collections.Generic
Imports System.DirectoryServices.AccountManagement
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    ''' <summary>
    ''' Logical folder which contains windows groups and is located
    ''' at '/acl/groups' path.
    ''' </summary>
    Public Class GroupFolder
        Inherits LogicalFolder
        Implements IPrincipalFolderAsync

        ''' <summary>
        ''' Path to folder which contains groups.
        ''' </summary>
        Public Shared PREFIX As String = AclFolder.PREFIX & "/groups"

        ''' <summary>
        ''' Gets path of folder which contains groups.
        ''' </summary>
        Public Shared ReadOnly Property PATH As String
            Get
                Return PREFIX & "/"
            End Get
        End Property

        ''' <summary>
        ''' Initializes a new instance of the <see cref="GroupFolder"/>  class.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        Public Sub New(context As DavContext)
            MyBase.New(context, "groups", PATH)
        End Sub

        Public Overrides Async Function GetChildrenAsync(properties As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
            Return Context.PrincipalOperation(Of IEnumerable(Of IHierarchyItemAsync))(AddressOf getGroups)
        End Function

        Public Async Function CreateFolderAsync(name As String) As Task(Of IPrincipalFolderAsync) Implements IPrincipalFolderAsync.CreateFolderAsync
            Throw New DavException("Creating folders is not implemented", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function CreatePrincipalAsync(name As String) As Task(Of IPrincipalAsync) Implements IPrincipalFolderAsync.CreatePrincipalAsync
            If Not PrincipalBase.IsValidUserName(name) Then
                Throw New DavException("Group name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim groupPrincipal As GroupPrincipal = New GroupPrincipal(Context.GetPrincipalContext())
            groupPrincipal.Name = name
            groupPrincipal.Save()
            Return New Group(groupPrincipal, Context)
        End Function

        Public Async Function FindPrincipalsByPropertyValuesAsync(propValues As IList(Of PropertyValue),
                                                                 props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.FindPrincipalsByPropertyValuesAsync
            Dim group As GroupPrincipal = New GroupPrincipal(Context.GetPrincipalContext())
            group.Name = "*"
            For Each v As PropertyValue In propValues
                If v.QualifiedName = PropertyName.DISPLAYNAME Then
                    group.Name = "*" & v.Value & "*"
                End If
            Next

            Dim searcher As PrincipalSearcher = New PrincipalSearcher(group)
            Return searcher.FindAll().Select(Function(u) New Group(CType(u, GroupPrincipal), Context)).Cast(Of IPrincipalAsync)()
        End Function

        Public Async Function GetPrincipalSearcheablePropertiesAsync() As Task(Of IEnumerable(Of PropertyDescription)) Implements IPrincipalFolderAsync.GetPrincipalSearcheablePropertiesAsync
            Return {New PropertyDescription With {.Name = PropertyName.DISPLAYNAME, .Description = "Principal name", .Lang = "en"}}
        End Function

        Public Async Function GetMatchingPrincipalsAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.GetMatchingPrincipalsAsync
            Dim user As User = User.FromName(Context.WindowsIdentity.Name, Context)
            Return Await user.GetGroupMembershipAsync()
        End Function

        Private Function getGroups() As IEnumerable(Of IHierarchyItemAsync)
            Dim insGroupPrincipal As GroupPrincipal = New GroupPrincipal(Context.GetPrincipalContext())
            insGroupPrincipal.Name = "*"
            Dim insPrincipalSearcher As PrincipalSearcher = New PrincipalSearcher(insGroupPrincipal)
            Dim r As PrincipalSearchResult(Of Principal) = insPrincipalSearcher.FindAll()
            Return r.Select(Function(g) New Group(CType(g, GroupPrincipal), Context)).Cast(Of IHierarchyItemAsync)().ToList()
        End Function
    End Class
End Namespace
