Imports System
Imports System.Collections.Generic
Imports System.Linq
Imports System.DirectoryServices.AccountManagement
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

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

        Public Shared Function FromName(name As String, context As DavContext) As User
            Dim principal As Principal
            Dim principalContext As PrincipalContext = context.GetPrincipalContext()
            Try
                If principalContext.ContextType = ContextType.Machine Then
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
                Return Nothing
            End Try

            Return New User(TryCast(principal, UserPrincipal), context)
        End Function

        Public Overrides Async Function SetGroupMembersAsync(members As IList(Of IPrincipalAsync)) As Task
            Throw New DavException("User objects can not contain other users.", DavStatus.CONFLICT)
        End Function

        Public Overrides Async Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipalAsync))
            Return New IPrincipalAsync(-1) {}
        End Function

        Public Overrides Function IsWellKnownPrincipal(wellknownPrincipal As WellKnownPrincipal) As Boolean
            Return wellknownPrincipal = WellKnownPrincipal.Unauthenticated AndAlso Context.AnonymousUser IsNot Nothing AndAlso Sid.Value = Context.AnonymousUser.User.Value
        End Function

        Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task
            If destFolder.Path <> New UserFolder(Context).Path Then
                Throw New DavException("Copying users is only allowed into the same folder", DavStatus.CONFLICT)
            End If

            If Not IsValidUserName(destName) Then
                Throw New DavException("User name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim newUser As UserPrincipal = New UserPrincipal(userPrincipal.Context) With {.Name = destName, .Description = userPrincipal.Description}
            Context.PrincipalOperation(AddressOf newUser.Save)
        End Function

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

        Public Overrides Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName))
            Return PrincipalProperties.ALL
        End Function

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
