Imports System
Imports System.Collections.Generic
Imports System.DirectoryServices
Imports System.DirectoryServices.AccountManagement
Imports System.Linq
Imports System.Security.Principal
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.CardDav
Imports CardDAVServer.FileSystemStorage.AspNet.CardDav
Imports CardDAVServer.FileSystemStorage.AspNet
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipalAsync

Namespace Acl

    ''' <summary>
    ''' Base class for users and groups.
    ''' </summary>
    Public MustInherit Class PrincipalBase
        Inherits Discovery
        Implements IPrincipal, IAddressbookPrincipalAsync

        ''' <summary>
        ''' Encoded path to the parent folder.
        ''' </summary>
        Private ReadOnly parentPath As String

        ''' <summary>
        ''' Initializes a new instance of the PrincipalBase class.
        ''' </summary>
        ''' <param name="principal">Instance of <see cref="Principal"/> .</param>
        ''' <param name="parentPath">Encoded path to parent folder.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
        Protected Sub New(principal As Principal, parentPath As String, context As DavContext)
            MyBase.New(context)
            Me.Principal = principal
            Me.parentPath = parentPath
            Me.Context = context
        End Sub

        ''' <summary>
        ''' Gets corresponding <see cref="Principal"/> .
        ''' </summary>
        Public Property Principal As Principal

        ''' <summary>
        ''' Gets principal name.
        ''' </summary>
        Public ReadOnly Property Name As String Implements IHierarchyItemAsync.Name
            Get
                Return Principal.SamAccountName
            End Get
        End Property

        ''' <summary>
        ''' Gets date when principal was created.
        ''' </summary>
        Public ReadOnly Property Created As DateTime Implements IHierarchyItemAsync.Created
            Get
                Dim o As Object = CType(Principal.GetUnderlyingObject(), DirectoryEntry).Properties("whenCreated").Value
                Return If(o IsNot Nothing, CDate(o), New DateTime(2000, 1, 1).ToUniversalTime())
            End Get
        End Property

        ''' <summary>
        ''' Gets date when principal was modified.
        ''' </summary>
        Public ReadOnly Property Modified As DateTime Implements IHierarchyItemAsync.Modified
            Get
                Dim o As Object = CType(Principal.GetUnderlyingObject(), DirectoryEntry).Properties("whenChanged").Value
                Return If(o IsNot Nothing, CDate(o), New DateTime(2000, 1, 1).ToUniversalTime())
            End Get
        End Property

        ''' <summary>
        ''' Gets principal's security identifier.
        ''' </summary>
        Public ReadOnly Property Sid As SecurityIdentifier
            Get
                Return Principal.Sid
            End Get
        End Property

        ''' <summary>
        ''' Gets encoded path to this principal.
        ''' </summary>
        Public MustOverride ReadOnly Property Path As String Implements IHierarchyItemAsync.Path

        ''' <summary>
        ''' Gets instance of <see cref="DavContext"/> .
        ''' </summary>
        Protected Property Context As DavContext

        ''' <summary>
        ''' Checks principal name for validity.
        ''' </summary>
        ''' <param name="name">Name to check.</param>
        ''' <returns>Whether principal name is valid.</returns>
        Public Shared Function IsValidUserName(name As String) As Boolean
            Dim invChars As Char() = {""""c, "/"c, "\"c, "["c, "]"c, ":"c, ";"c, "|"c, "="c, ","c, "+"c, "*"c, "?"c, "<"c, ">"c}
            Return Not invChars.Where(Function(c) name.Contains(c)).Any()
        End Function

        ''' <summary>
        ''' Gets groups to which this principal belongs.
        ''' </summary>
        ''' <returns>Enumerable with groups.</returns>
        Public Async Function GetGroupMembershipAsync() As Task(Of IEnumerable(Of IPrincipal)) Implements IPrincipalAsync.GetGroupMembershipAsync
            Return Principal.GetGroups().Select(Function(group) CType(New Group(CType(group, GroupPrincipal), Context), IPrincipal))
        End Function

        ''' <summary>
        ''' Deletes the principal.
        ''' </summary>
        ''' <param name="multistatus">We don't use it currently as there are no child objects.</param>
        Public Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            Context.PrincipalOperation(AddressOf Principal.Delete)
        End Function

        ''' <summary>
        ''' Renames principal.
        ''' </summary>
        ''' <param name="destFolder">We don't use it as moving groups to different folder is not supported.</param>
        ''' <param name="destName">New name.</param>
        ''' <param name="multistatus">We don't use it as there're no child objects.</param>
        Public Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
            If destFolder.Path <> parentPath Then
                Throw New DavException("Moving principals is only allowed into the same folder", DavStatus.CONFLICT)
            End If

            If Not IsValidUserName(destName) Then
                Throw New DavException("Principal name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Context.PrincipalOperation(Sub() CType(Principal.GetUnderlyingObject(), DirectoryEntry).Rename(destName))
        End Function

        Public MustOverride Function SetGroupMembersAsync(members As IList(Of IPrincipal)) As Task Implements IPrincipalAsync.SetGroupMembersAsync

        Public MustOverride Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipal)) Implements IPrincipalAsync.GetGroupMembersAsync

        Public MustOverride Function IsWellKnownPrincipal(wellknownPrincipal As WellKnownPrincipal) As Boolean Implements IPrincipalAsync.IsWellKnownPrincipal

        Public MustOverride Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync

        Public MustOverride Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync

        Public MustOverride Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue),
                                                          delProps As IList(Of PropertyName),
                                                          multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync

        Public MustOverride Function CopyToAsync(destFolder As IItemCollectionAsync,
                                                destName As String,
                                                deep As Boolean,
                                                multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
    End Class
End Namespace
