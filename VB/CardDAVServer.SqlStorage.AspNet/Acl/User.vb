Imports System
Imports System.Web
Imports System.Collections.Generic
Imports System.Web.Security
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.Server

Namespace Acl

    ''' <summary>
    ''' This class represents user principal in WebDAV hierarchy. 
    ''' Instances of this class correspond to the following path: [DAVLocation]/acl/users/[UserID].
    ''' </summary>
    Public Class User
        Inherits Discovery
        Implements IAddressbookPrincipal

        Private ReadOnly email As String

        Public Shared Async Function GetUserAsync(context As DavContext, userId As String) As Task(Of User)
            ' Loged-in user can access only his own account data.
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
        Public Property Name As String Implements IHierarchyItemBase.Name

        ''' <summary>
        ''' Gets encoded path to this principal.
        ''' </summary>
        Public Property Path As String Implements IHierarchyItemBase.Path

        ''' <summary>
        ''' Gets date when principal was created.
        ''' </summary>
        Public Property Created As DateTime Implements IHierarchyItemBase.Created

        ''' <summary>
        ''' Gets date when principal was modified.
        ''' </summary>
        Public Property Modified As DateTime Implements IHierarchyItemBase.Modified

        ''' <summary>
        ''' Creates new user as copy of this one.
        ''' </summary>
        ''' <param name="destFolder">Is not used as there's no more locations a user can be copied.</param>
        ''' <param name="destName">New user name.</param>
        ''' <param name="deep">Whether to copy children - is not user.</param>
        ''' <param name="multistatus">Is not used as there's no children.</param>
        Public Async Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task Implements IHierarchyItem.CopyToAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Renames principal.
        ''' </summary>
        ''' <param name="destFolder">We don't use it as moving groups to different folder is not supported.</param>
        ''' <param name="destName">New name.</param>
        ''' <param name="multistatus">We don't use it as there're no child objects.</param>
        Public Async Function MoveToAsync(destFolder As IItemCollection, destName As String, multistatus As MultistatusException) As Task Implements IHierarchyItem.MoveToAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Deletes the principal.
        ''' </summary>
        ''' <param name="multistatus">We don't use it currently as there are no child objects.</param>
        Public Async Function DeleteAsync(multistatus As MultistatusException) As Task Implements IHierarchyItem.DeleteAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' Retrieves properties of the user.
        ''' </summary>
        ''' <param name="props">Properties to retrieve.</param>
        ''' <param name="allprop">Whether all properties shall be retrieved.</param>
        ''' <returns>Property values.</returns>
        Public Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue)) Implements IHierarchyItem.GetPropertiesAsync
            Return New PropertyValue(-1) {}
        End Function

        ''' <summary>
        ''' Retrieves names of dead properties.
        ''' </summary>
        ''' <returns>Dead propery names.</returns>
        Public Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName)) Implements IHierarchyItem.GetPropertyNamesAsync
            Return New PropertyName(-1) {}
        End Function

        ''' <summary>
        ''' Updates dead properties.
        ''' </summary>
        ''' <param name="setProps">Properties to set.</param>
        ''' <param name="delProps">Properties to delete.</param>
        ''' <param name="multistatus">Here we report problems with properties.</param>
        Public Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task Implements IHierarchyItem.UpdatePropertiesAsync
            Throw New DavException("Not implemented.", DavStatus.NOT_IMPLEMENTED)
        End Function

        ''' <summary>
        ''' We don't implement it - users doesn't support setting members.
        ''' The method is here because from WebDAV perspective there's no difference
        ''' between users and groups.
        ''' </summary>
        ''' <param name="members">Members of the group.</param>
        Public Async Function SetGroupMembersAsync(members As IList(Of IPrincipal)) As Task Implements IPrincipal.SetGroupMembersAsync
            Throw New DavException("User objects can not contain other users.", DavStatus.CONFLICT)
        End Function

        ''' <summary>
        ''' Retrieves principal members. Users have no members, so return empty list.
        ''' </summary>
        ''' <returns>Principal members.</returns>
        Public Async Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipal)) Implements IPrincipal.GetGroupMembersAsync
            Return New IPrincipal(-1) {}
        End Function

        ''' <summary>
        ''' Gets groups to which this principal belongs.
        ''' </summary>
        ''' <returns>Enumerable with groups.</returns>
        Public Async Function GetGroupMembershipAsync() As Task(Of IEnumerable(Of IPrincipal)) Implements IPrincipal.GetGroupMembershipAsync
            Return New IPrincipal(-1) {}
        End Function

        ''' <summary>
        ''' Checks whether this user is of well-known type.
        ''' </summary>
        ''' <param name="wellknownPrincipal">Type to check.</param>
        ''' <returns><c>true</c> if the user is of specified well-known type.</returns>
        Public Function IsWellKnownPrincipal(wellknownPrincipal As WellKnownPrincipal) As Boolean Implements IPrincipal.IsWellKnownPrincipal
            Return(wellknownPrincipal = WellKnownPrincipal.Unauthenticated) AndAlso (Name = "Anonymous")
        End Function
    End Class
End Namespace
