Imports System.Web.Hosting
Imports System.DirectoryServices.AccountManagement
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    Friend Module AclFactory

        ''' <summary>
        ''' Gets object representing ACL folder/user/group.
        ''' </summary>
        ''' <param name="path">Relative path requested.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/>  class.</param>
        ''' <returns>Object implemening LogicalFolder/IPrincipalFolder</returns>
        Friend Async Function GetAclItemAsync(context As DavContext, path As String) As Task(Of IHierarchyItemAsync)
            'If this is /acl - return fake folder which contains users and groups.
            If path = AclFolder.PREFIX Then
                Return New AclFolder(context)
            End If

            'if this is /acl/users - return fake folder which contains users.
            If path = UserFolder.PREFIX Then
                Return New UserFolder(context)
            End If

            'if this is /acl/groups - return fake folder which contains groups.
            If path = GroupFolder.PREFIX Then
                Return New GroupFolder(context)
            End If

            'if this is /acl/users/<user name> - return instance of User.
            If path.StartsWith(UserFolder.PATH) Then
                Dim name As String = EncodeUtil.DecodeUrlPart(path.Substring(UserFolder.PATH.Length))
                'we don't need an exception here - so check for validity.
                If PrincipalBase.IsValidUserName(name) Then
                    Return User.FromName(name, context)
                End If
            End If

            'if this is /acl/groups/<group name> - return instance of Group.
            If path.StartsWith(GroupFolder.PATH) Then
                Dim name As String = EncodeUtil.DecodeUrlPart(path.Substring(GroupFolder.PATH.Length))
                If PrincipalBase.IsValidUserName(name) Then
                    Return Group.FromName(name, context)
                End If
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Creates <see cref="Group"/>  or <see cref="User"/>  for windows SID.
        ''' </summary>
        ''' <param name="sid">Windows SID.</param>
        ''' <param name="context">Instance of <see cref="DavContext"/> .</param>
        ''' <returns>Corresponding <see cref="User"/>  or <see cref="Group"/>  or <c>null</c> if there's no user
        ''' or group which correspond to specified sid.</returns>
        Function GetPrincipalFromSid(sid As String, context As DavContext) As IPrincipalAsync
            Using HostingEnvironment.Impersonate()
                ' This code runs as the application pool user
                Dim pr As Principal = Principal.FindByIdentity(context.GetPrincipalContext(), IdentityType.Sid, sid)
                If pr Is Nothing Then Return Nothing
                Return If(TypeOf pr Is GroupPrincipal, CType(New Group(CType(pr, GroupPrincipal), context), IPrincipalAsync), New User(CType(pr, UserPrincipal), context))
            End Using
        End Function
    End Module
End Namespace
