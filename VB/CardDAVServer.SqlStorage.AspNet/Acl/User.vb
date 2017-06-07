Imports System
Imports System.Web
Imports System.Collections.Generic
Imports System.Web.Security
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports ITHit.WebDAV.Server.CardDav

Namespace Acl

    ''' <summary>
    ''' This class represents user principal in WebDAV hierarchy. 
    ''' Instances of this class correspond to the following path: [DAVLocation]/acl/users/[UserID].
    ''' </summary>
    Public Class User
        Inherits Discovery
        Implements IAddressbookPrincipalAsync

        Private ReadOnly email As String

        Public Shared Async Function GetUserAsync(context As DavContext, userId As String) As Task(Of User)
            If Not context.UserId.Equals(userId, StringComparison.InvariantCultureIgnoreCase) Then Throw New DavException("Forbidden.", DavStatus.FORBIDDEN)
            Dim user As MembershipUser = Membership.GetUser(userId)
            Return New User(context, userId, user.UserName, user.Email, New DateTime(2000, 1, 1), New DateTime(2000, 1, 1))
        End Function

        ''' <summary>
        ''' Creates instance of User class.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="userId">ID of this user</param>
        ''' <remarks>
        ''' This consturctor is called when user URL is required, typically when discovering user calendars, 
        ''' no need to populate all properties, only user ID is required.
        ''' </remarks>
        Public Sub New(context As DavContext, userId As String)
            MyClass.New(context, userId, Nothing, Nothing, New DateTime(2000, 1, 1), New DateTime(2000, 1, 1))
        End Sub

        ''' <summary>
        ''' Creates instance of User class.
        ''' </summary>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <param name="userId">ID of this user</param>
        ''' <param name="name">User name.</param>
        ''' <param name="email">User e-mail.</param>
        ''' <param name="created">Date when this item was created.</param>
        ''' <param name="modified">Date when this item was modified.</param>
        Public Sub New(context As DavContext, userId As String, name As String, email As String, created As DateTime, modified As DateTime)
            MyBase.New(context)
            Me.Name = name
            Me.email = email
            Me.Path = UsersFolder.UsersFolderPath & EncodeUtil.EncodeUrlPart(userId)
            Me.Created = created
            Me.Modified = modified
        End Sub

        ''' <summary>
        ''' Gets principal name.
        ''' </summary>
        Public Property Name As String Implements IHierarchyItemAsync.Name

        ''' <summary>
        ''' Gets encoded path to this principal.
        ''' </summary>
        Public Property Path As String Implements IHierarchyItemAsync.Path

        ''' <summary>
        ''' Gets date when principal was created.
        ''' </summary>
        Public Property Created As DateTime Implements IHierarchyItemAsync.Created

        ''' <summary>
        ''' Gets date when principal was modified.
        ''' </summary>
        Public Property Modified As DateTime Implements IHierarchyItemAsync.Modified

        Public Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.CopyToAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function MoveToAsync(destFolder As IItemCollectionAsync, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.MoveToAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.DeleteAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItemAsync.GetPropertiesAsync
            Return New PropertyValue(-1) {}
        End Function

        Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItemAsync.GetPropertyNamesAsync
            Return New PropertyName(-1) {}
        End Function

        Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItemAsync.UpdatePropertiesAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        Public Async Function SetGroupMembersAsync(members As IList(Of IPrincipalAsync)) As Task Implements IPrincipalAsync.SetGroupMembersAsync
            Throw New DavException("User objects can not contain other users.", DavStatus.CONFLICT)
        End Function

        Public Async Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalAsync.GetGroupMembersAsync
            Return New IPrincipalAsync(-1) {}
        End Function

        Public Async Function GetGroupMembershipAsync() As Task(Of IEnumerable(Of IPrincipalAsync)) Implements IPrincipalAsync.GetGroupMembershipAsync
            Return New IPrincipalAsync(-1) {}
        End Function

        Public Function IsWellKnownPrincipal(wellknownPrincipal As WellKnownPrincipal) As Boolean Implements IPrincipalAsync.IsWellKnownPrincipal
            Return(wellknownPrincipal = WellKnownPrincipal.Unauthenticated) AndAlso (Name = "Anonymous")
        End Function
    End Class
End Namespace
