Imports System
Imports System.Collections.Generic
Imports System.DirectoryServices.AccountManagement
Imports System.Linq
Imports System.Security.Principal
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipalAsync

Namespace Acl

    ''' <summary>
    ''' Item which represents user group.
    ''' These items are located under '/acl/group/' folder.
    ''' </summary>
    Public Class Group
        Inherits PrincipalBase

        ''' <summary>
        ''' Corresponding .net GroupPrincipal object.
        ''' </summary>
        Private ReadOnly groupPrincipal As GroupPrincipal

        ''' <summary>
        ''' Initializes a new instance of the Group class.
        ''' </summary>
        ''' <param name="groupPrincipal">Corresponding .net GroupPrincipal object.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
        Public Sub New(groupPrincipal As GroupPrincipal, context As DavContext)
            MyBase.New(groupPrincipal, GroupFolder.PATH, context)
            Me.groupPrincipal = groupPrincipal
        End Sub

        ''' <summary>
        ''' Group path.
        ''' </summary>
        Public Overrides ReadOnly Property Path As String
            Get
                Return GroupFolder.PREFIX & "/" & EncodeUtil.EncodeUrlPart(Name)
            End Get
        End Property

        Public Shared Function FromName(name As String, context As DavContext) As Group
            Dim principal As Principal
            Dim principalContext As PrincipalContext = context.GetPrincipalContext()
            Try
                If principalContext.ContextType = ContextType.Machine Then
                    Dim principalToSearch As GroupPrincipal = New GroupPrincipal(principalContext)
                    principalToSearch.SamAccountName = name
                    principal = New PrincipalSearcher(principalToSearch).FindOne()
                Else
                    ' search domain
                    principal = GroupPrincipal.FindByIdentity(principalContext, IdentityType.SamAccountName, name)
                End If

                If(principal Is Nothing) OrElse Not(TypeOf principal Is GroupPrincipal) Then
                    Return Nothing
                End If
            Catch __unusedPrincipalOperationException1__ As PrincipalOperationException
                Return Nothing
            End Try

            Return New Group(TryCast(principal, GroupPrincipal), context)
        End Function

        Public Overrides Async Function SetGroupMembersAsync(newMembers As IList(Of IPrincipal)) As Task
            Dim members As PrincipalCollection = groupPrincipal.Members
            Dim toDelete As IEnumerable(Of Principal) = members.Where(Function(m) Not newMembers.Where(Function(nm) CType(nm, PrincipalBase).Principal.Sid.Value = m.Sid.Value).Any()).ToList()
            For Each p As Principal In toDelete
                groupPrincipal.Members.Remove(p)
            Next

            Dim toAdd As IEnumerable(Of IPrincipal) = newMembers.Where(Function(nm) Not members.Where(Function(m) m.Sid.Value = CType(nm, PrincipalBase).Principal.Sid.Value).Any()).ToList()
            For Each p As PrincipalBase In toAdd
                groupPrincipal.Members.Add(p.Principal)
            Next

            Context.PrincipalOperation(AddressOf groupPrincipal.Save)
        End Function

        Public Overrides Async Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipal))
            Return groupPrincipal.GetMembers().Select(AddressOf convertPrincipal)
        End Function

        Public Overrides Function IsWellKnownPrincipal(wellKnown As WellKnownPrincipal) As Boolean
            Select Case wellKnown
                Case WellKnownPrincipal.All
                    Return groupPrincipal.Sid.IsWellKnown(WellKnownSidType.WorldSid)
                Case WellKnownPrincipal.Authenticated
                    Return groupPrincipal.Sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid)
            End Select

            Return False
        End Function

        Public Overrides Async Function CopyToAsync(destFolder As IItemCollectionAsync, destName As String, deep As Boolean, multistatus As MultistatusException) As Task
            If destFolder.Path <> New GroupFolder(Context).Path Then
                Throw New DavException("Copying groups is only allowed into the same folder", DavStatus.CONFLICT)
            End If

            If Not IsValidUserName(destName) Then
                Throw New DavException("User name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim newGroup As GroupPrincipal = New GroupPrincipal(groupPrincipal.Context) With {.Name = destName, .Description = groupPrincipal.Description}
            Context.PrincipalOperation(AddressOf newGroup.Save)
        End Function

        Public Overrides Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue))
            Dim propsToGet As IEnumerable(Of PropertyName) = If(Not allprop, props, props.Union({PrincipalProperties.Description}))
            Dim propList As List(Of PropertyValue) = New List(Of PropertyValue)()
            For Each propName As PropertyName In propsToGet
                If propName = PrincipalProperties.Description Then
                    propList.Add(New PropertyValue(propName, groupPrincipal.Description))
                End If
            Next

            Return propList
        End Function

        Public Overrides Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName))
            Return {PrincipalProperties.Description}
        End Function

        Public Overrides Async Function UpdatePropertiesAsync(setProps As IList(Of PropertyValue), delProps As IList(Of PropertyName), multistatus As MultistatusException) As Task
            For Each prop As PropertyValue In setProps
                If prop.QualifiedName = PrincipalProperties.Description Then
                    groupPrincipal.Description = prop.Value
                Else
                    'Property is not supported. Tell client about this.
                    multistatus.AddInnerException(Path,
                                                 prop.QualifiedName,
                                                 New DavException("Property was not found.", DavStatus.NOT_FOUND))
                End If
            Next

            For Each propertyName As PropertyName In delProps
                multistatus.AddInnerException(Path,
                                             propertyName,
                                             New DavException("It is not allowed to remove properties on principal objects.",
                                                                                  DavStatus.FORBIDDEN))
            Next

            Context.PrincipalOperation(AddressOf groupPrincipal.Save)
        End Function

        Private Function convertPrincipal(principal As Principal) As IPrincipal
            If TypeOf principal Is UserPrincipal Then
                Return New User(CType(principal, UserPrincipal), Context)
            End If

            Return New Group(CType(principal, GroupPrincipal), Context)
        End Function
    End Class
End Namespace
