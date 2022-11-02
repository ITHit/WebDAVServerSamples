Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.DirectoryServices.AccountManagement
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal

Namespace Acl

    ''' <summary>
    ''' Represents windows user in WebDAV hierarchy.
    ''' </summary>
    Public Class User
        Inherits PrincipalBase

        Friend userPrincipal As UserPrincipal

        Public Sub New(userPrincipal As UserPrincipal, context As DavContext)
            MyBase.New(userPrincipal, UserFolder.PATH, context)
            If userPrincipal Is Nothing Then
                Throw New ArgumentNullException("userPrincipal")
            End If

            Me.userPrincipal = userPrincipal
        End Sub

        ''' <summary>
        ''' Creates <see cref="User"/>  instance from name.
        ''' </summary>
        ''' <param name="name">User name.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
        ''' <returns>Instance of <see cref="User"/>  or <c>null</c> if user is not found.</returns>
        Public Shared Function FromName(name As String, context As DavContext) As User
            ' Calling FindByIdentity on a machine that is not on a domain is 
            ' very slow (even with new PrincipalContext(ContextType.Machine)).
            ' using PrincipalSearcher on a local machine and FindByIdentity on a domain.
            Dim principal As Principal
            Dim principalContext As PrincipalContext = context.GetPrincipalContext()
            Try
                If principalContext.ContextType = ContextType.Machine Then
                    ' search local machine
                    Dim principalToSearch As UserPrincipal = New UserPrincipal(principalContext)
                    principalToSearch.SamAccountName = name
                    principal = New PrincipalSearcher(principalToSearch).FindOne()
                Else
                    ' search domain
                    principal = UserPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, name)
                End If

                If(principal Is Nothing) OrElse Not(TypeOf principal Is UserPrincipal) Then
                    Return Nothing
                End If
            Catch __unusedPrincipalOperationException1__ As PrincipalOperationException
                'This exception is thrown if user cannot be found.
                Return Nothing
            End Try

            Return New User(TryCast(principal, UserPrincipal), context)
        End Function

        ''' <summary>
        ''' We don't implement it - users doesn't support setting members.
        ''' The method is here because from WebDAV perspective there's no difference
        ''' between users and groups.
        ''' </summary>
        ''' <param name="members">Members of the group.</param>
        Public Overrides Async Function SetGroupMembersAsync(members As IList(Of IPrincipal)) As Task
            Throw New DavException("User objects can not contain other users.", DavStatus.CONFLICT)
        End Function

        ''' <summary>
        ''' Retrieves principal members. Users have no members, so return empty list.
        ''' </summary>
        ''' <returns>Principal members.</returns>
        Public Overrides Async Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipal))
            Return New IPrincipal(-1) {}
        End Function

        ''' <summary>
        ''' Checks whether this user is of well-known type.
        ''' </summary>
        ''' <param name="wellknownPrincipal">Type to check.</param>
        ''' <returns><c>true</c> if the user is of specified well-known type.</returns>
        Public Overrides Function IsWellKnownPrincipal(wellknownPrincipal As WellKnownPrincipal) As Boolean
            Return wellknownPrincipal = WellKnownPrincipal.Unauthenticated AndAlso Context.AnonymousUser IsNot Nothing AndAlso Sid.Value = Context.AnonymousUser.User.Value
        End Function

        ''' <summary>
        ''' Creates new user as copy of this one.
        ''' </summary>
        ''' <param name="destFolder">Is not used as there's no more locations a user can be copied.</param>
        ''' <param name="destName">New user name.</param>
        ''' <param name="deep">Whether to copy children - is not user.</param>
        ''' <param name="multistatus">Is not used as there's no children.</param>
        Public Overrides Async Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task
            If destFolder.Path <> New UserFolder(Context).Path Then
                Throw New DavException("Copying users is only allowed into the same folder", DavStatus.CONFLICT)
            End If

            If Not IsValidUserName(destName) Then
                Throw New DavException("User name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim newUser As UserPrincipal = New UserPrincipal(userPrincipal.Context) With {.Name = destName,
                                                                                    .Description = userPrincipal.Description
                                                                                    }
            Context.PrincipalOperation(AddressOf newUser.Save)
        End Function

        ''' <summary>
        ''' Retrieves properties of the user.
        ''' </summary>
        ''' <param name="props">Properties to retrieve.</param>
        ''' <param name="allprop">Whether all properties shall be retrieved.</param>
        ''' <returns>Property values.</returns>
        Public Overrides Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue))
            Dim propsToGet As IEnumerable(Of PropertyName) = If(Not allprop, props, props.Union(PrincipalProperties.ALL))
            Dim propValues As List(Of PropertyValue) = New List(Of PropertyValue)()
            For Each propName As PropertyName In propsToGet
                If propName = PrincipalProperties.FullName Then
                    propValues.Add(New PropertyValue(propName, userPrincipal.DisplayName))
                End If

                If propName = PrincipalProperties.Description Then
                    propValues.Add(New PropertyValue(propName, userPrincipal.Description))
                End If
            Next

            Return propValues
        End Function

        ''' <summary>
        ''' Retrieves names of dead properties.
        ''' </summary>
        ''' <returns>Dead propery names.</returns>
        Public Overrides Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName))
            Return PrincipalProperties.ALL
        End Function

        ''' <summary>
        ''' Updates dead properties.
        ''' </summary>
        ''' <param name="setProps">Properties to set.</param>
        ''' <param name="delProps">Properties to delete.</param>
        ''' <param name="multistatus">Here we report problems with properties.</param>
        Public Overrides Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task
            For Each prop As PropertyValue In setProps
                If prop.QualifiedName = PrincipalProperties.FullName Then
                    userPrincipal.DisplayName = prop.Value
                ElseIf prop.QualifiedName = PrincipalProperties.Description Then
                    userPrincipal.Description = prop.Value
                Else
                    multistatus.AddInnerException(Path,
                                                 prop.QualifiedName,
                                                 New DavException("The property was not found", DavStatus.NOT_FOUND))
                End If
            Next

            For Each p As PropertyName In delProps
                multistatus.AddInnerException(Path,
                                             p,
                                             New DavException("Principal properties can not be deleted.", DavStatus.FORBIDDEN))
            Next

            Context.PrincipalOperation(AddressOf userPrincipal.Save)
        End Function

        Public Overrides ReadOnly Property Path As String
            Get
                Return UserFolder.PREFIX & "/" & userPrincipal.SamAccountName
            End Get
        End Property
    End Class
End Namespace
