Imports System.Web.Hosting
Imports System.DirectoryServices.AccountManagement
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Acl

Namespace Acl

    Friend Module AclFactory

        Friend Async Function GetAclItemAsync(context As DavContext, path As String) As Task(Of IHierarchyItemAsync)
            If path = AclFolder.PREFIX Then
                Return New AclFolder(context)
            End If

            If path = UserFolder.PREFIX Then
                Return New UserFolder(context)
            End If

            If path = GroupFolder.PREFIX Then
                Return New GroupFolder(context)
            End If

            If path.StartsWith(UserFolder.PATH) Then
                Dim name As String = EncodeUtil.DecodeUrlPart(path.Substring(UserFolder.PATH.Length))
                If PrincipalBase.IsValidUserName(name) Then
                    Return User.FromName(name, context)
                End If
            End If

            If path.StartsWith(GroupFolder.PATH) Then
                Dim name As String = EncodeUtil.DecodeUrlPart(path.Substring(GroupFolder.PATH.Length))
                If PrincipalBase.IsValidUserName(name) Then
                    Return Group.FromName(name, context)
                End If
            End If

            Return Nothing
        End Function

        Function GetPrincipalFromSid(sid As String, context As DavContext) As IPrincipalAsync
            Using HostingEnvironment.Impersonate()
                Dim pr As Principal = Principal.FindByIdentity(context.GetPrincipalContext(), IdentityType.Sid, sid)
                If pr Is Nothing Then Return Nothing
                Return If(TypeOf pr Is GroupPrincipal, CType(New Group(CType(pr, GroupPrincipal), context), IPrincipalAsync), New User(CType(pr, UserPrincipal), context))
            End Using
        End Function
    End Module
End Namespace
