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

        ''' <summary>
        ''' Retrieves list of windows groups.
        ''' </summary>
        ''' <param name="properties">Properties which will be requested from the item by the engine later.</param>
        ''' <returns>Enumerable with groups.</returns>
        Public Overrides Async Function GetChildrenAsync(properties As IList(Of PropertyName)) As Task(Of IEnumerable(Of IHierarchyItemAsync)) Implements IItemCollectionAsync.GetChildrenAsync
            Return Context.PrincipalOperation(Of IEnumerable(Of IHierarchyItemAsync))(AddressOf getGroups)
        End Function

        ''' <summary>
        ''' Required by interface. However we don't allow creating folders inside this folder.
        ''' </summary>
        ''' <param name="name">New folder name.</param>
        ''' <returns>New folder.</returns>
        Public Async Function CreateFolderAsync(name As String) As Task(Of IPrincipalFolderAsync) Implements IPrincipalFolderAsync.CreateFolderAsync
            'Creating folders inside this folder is not supported.
            Throw New DavException("Creating folders is not implemented", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Creates group with specified name.
        ''' </summary>
        ''' <param name="name">Group name.</param>
        ''' <returns>Newly created group.</returns>
        Public Async Function CreatePrincipalAsync(name As String) As Task(Of IPrincipalAsync) Implements IPrincipalFolderAsync.CreatePrincipalAsync
            If Not PrincipalBase.IsValidUserName(name) Then
                Throw New DavException("Group name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim groupPrincipal As GroupPrincipal = New GroupPrincipal(Context.GetPrincipalContext())
            groupPrincipal.Name = name
            groupPrincipal.Save()
            Return New Group(groupPrincipal, Context)
        End Function

        ''' <summary>
        ''' Finds groups which have matching property values.
        ''' </summary>
        ''' <param name="propValues">Property values which group must have.</param>
        ''' <param name="props">Properties that will be retrieved later for the group found.</param>
        ''' <returns>Groups which have matching property.</returns>
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

        ''' <summary>
        ''' Returns properties which can be used in <see cref="FindPrincipalsByPropertyValuesAsync"/>  method.
        ''' </summary>
        ''' <returns>List of property description.</returns>
        Public Async Function GetPrincipalSearcheablePropertiesAsync() As Task(Of IEnumerable(Of PropertyDescription)) Implements IPrincipalFolderAsync.GetPrincipalSearcheablePropertiesAsync
            Return {New PropertyDescription With {.Name = PropertyName.DISPLAYNAME,
                                            .Description = "Principal name",
                                            .Lang = "en"}}
        End Function

        ''' <summary>
        ''' Returns all groups current user is member of.
        ''' </summary>
        ''' <param name="props">Properties that will be asked later from the groups returned.</param>
        ''' <returns>Enumerable with groups.</returns>
        Public Async Function GetMatchingPrincipalsAsync(props As IList(Of PropertyName)) As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalFolderAsync.GetMatchingPrincipalsAsync
            Dim user As User = User.FromName(Context.WindowsIdentity.Name, Context)
            Return Await user.GetGroupMembershipAsync()
        End Function

        ''' <summary>
        ''' Retrieves all groups in computer/domain.
        ''' </summary>
        ''' <returns>Enumerable with groups.</returns>
        Private Function getGroups() As IEnumerable(Of IHierarchyItemAsync)
            Dim insGroupPrincipal As GroupPrincipal = New GroupPrincipal(Context.GetPrincipalContext())
            insGroupPrincipal.Name = "*"
            Dim insPrincipalSearcher As PrincipalSearcher = New PrincipalSearcher(insGroupPrincipal)
            Dim r As PrincipalSearchResult(Of Principal) = insPrincipalSearcher.FindAll()
            Return r.Select(Function(g) New Group(CType(g, GroupPrincipal), Context)).Cast(Of IHierarchyItemAsync)().ToList()
        End Function
    End Class
End Namespace
