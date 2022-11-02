Imports System
Imports System.Collections.Generic
Imports System.DirectoryServices.AccountManagement
Imports System.Linq
Imports System.Security.Principal
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl
Imports IPrincipal = ITHit.WebDAV.Server.Acl.IPrincipal

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

        ''' <summary>
        ''' Creates group item from name.
        ''' </summary>
        ''' <param name="name">Group name.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/></param>
        ''' <returns><see cref="Group"/>  instance.</returns>
        Public Shared Function FromName(name As String, context As DavContext) As Group
            ' Calling FindByIdentity on a machine that is not on a domain is 
            ' very slow (even with new PrincipalContext(ContextType.Machine)).
            ' using PrincipalSearcher on a local machine and FindByIdentity on a domain.
            Dim principal As Principal
            Dim principalContext As PrincipalContext = context.GetPrincipalContext()
            Try
                If principalContext.ContextType = ContextType.Machine Then
                    ' search local machine
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
                'This exception is thrown if user cannot be found.
                Return Nothing
            End Try

            Return New Group(TryCast(principal, GroupPrincipal), context)
        End Function

        ''' <summary>
        ''' Sets members of the group.
        ''' </summary>
        ''' <param name="newMembers">List of group members.</param>
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

        ''' <summary>
        ''' Retrieves members of the group.
        ''' </summary>
        ''' <returns>Enumerable of group members.</returns>
        Public Overrides Async Function GetGroupMembersAsync() As Task(Of IEnumerable(Of IPrincipal))
            Return groupPrincipal.GetMembers().Select(AddressOf convertPrincipal)
        End Function

        ''' <summary>
        ''' Determines whether this is a wellknown group of <paramref name="wellKnown"/>  type.
        ''' </summary>
        ''' <param name="wellKnown">Type of wellknown pricipal to check.</param>
        ''' <returns><c>true</c> if the group is of questioned wellknown type.</returns>
        Public Overrides Function IsWellKnownPrincipal(wellKnown As WellKnownPrincipal) As Boolean
            Select Case wellKnown
                Case WellKnownPrincipal.All
                    Return groupPrincipal.Sid.IsWellKnown(WellKnownSidType.WorldSid)
                Case WellKnownPrincipal.Authenticated
                    Return groupPrincipal.Sid.IsWellKnown(WellKnownSidType.AuthenticatedUserSid)
            End Select

            Return False
        End Function

        ''' <summary>
        ''' Copies this item into <paramref name="destFolder"/>  and renames it to <paramref name="destName"/> .
        ''' </summary>
        ''' <param name="destFolder">Folder to copy this item to.</param>
        ''' <param name="destName">New name of the item.</param>
        ''' <param name="deep">Whether child objects shall be copied.</param>
        ''' <param name="multistatus">Multistatus to populate with errors.</param>
        Public Overrides Async Function CopyToAsync(destFolder As IItemCollection, destName As String, deep As Boolean, multistatus As MultistatusException) As Task
            If destFolder.Path <> New GroupFolder(Context).Path Then
                Throw New DavException("Copying groups is only allowed into the same folder", DavStatus.CONFLICT)
            End If

            If Not IsValidUserName(destName) Then
                Throw New DavException("User name contains invalid characters", DavStatus.FORBIDDEN)
            End If

            Dim newGroup As GroupPrincipal = New GroupPrincipal(groupPrincipal.Context) With {.Name = destName,
                                                                                        .Description = groupPrincipal.Description
                                                                                        }
            Context.PrincipalOperation(AddressOf newGroup.Save)
        End Function

        ''' <summary>
        ''' Retrieves dead properties of the group.
        ''' </summary>
        ''' <param name="props">Properties to retrieve.</param>
        ''' <param name="allprop">Whether all properties shall be retrieved.</param>
        ''' <returns>Values of requested properties.</returns>
        Public Overrides Async Function GetPropertiesAsync(props As IList(Of PropertyName), allprop As Boolean) As Task(Of IEnumerable(Of PropertyValue))
            Dim propsToGet As IEnumerable(Of PropertyName) = If(Not allprop, props, props.Union({PrincipalProperties.Description}))
            Dim propList As List(Of PropertyValue) = New List(Of PropertyValue)()
            For Each propName As PropertyName In propsToGet
                'we support only description.
                If propName = PrincipalProperties.Description Then
                    propList.Add(New PropertyValue(propName, groupPrincipal.Description))
                End If
            Next

            Return propList
        End Function

        ''' <summary>
        ''' Names of dead properties. Only description is supported for windows groups.
        ''' </summary>
        ''' <returns>List of dead property names.</returns>
        Public Overrides Async Function GetPropertyNamesAsync() As Task(Of IEnumerable(Of PropertyName))
            Return {PrincipalProperties.Description}
        End Function

        ''' <summary>
        ''' Update dead properties.
        ''' Currently on description is supported.
        ''' </summary>
        ''' <param name="setProps">Dead properties to be set.</param>
        ''' <param name="delProps">Dead properties to be deleted.</param>
        ''' <param name="multistatus">Multistatus with details for every failed property.</param>
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

            'We cannot delete any properties from group, because it is windows object.
            For Each propertyName As PropertyName In delProps
                multistatus.AddInnerException(Path,
                                             propertyName,
                                             New DavException("It is not allowed to remove properties on principal objects.",
                                                                                  DavStatus.FORBIDDEN))
            Next

            Context.PrincipalOperation(AddressOf groupPrincipal.Save)
        End Function

        ''' <summary>
        ''' Converts .net <see cref="Principal"/>  to either <see cref="Group"/>  or <see cref="User"/> .
        ''' </summary>
        ''' <param name="principal">Principal to convert.</param>
        ''' <returns>Either user or group.</returns>
        Private Function convertPrincipal(principal As Principal) As IPrincipal
            If TypeOf principal Is UserPrincipal Then
                Return New User(CType(principal, UserPrincipal), Context)
            End If

            Return New Group(CType(principal, GroupPrincipal), Context)
        End Function
    End Class
End Namespace
