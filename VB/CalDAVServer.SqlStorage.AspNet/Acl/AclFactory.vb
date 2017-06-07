Imports System
Imports System.Linq
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server

Namespace Acl

    ''' <summary>
    ''' Gets ACL principals and folders by provided path.
    ''' </summary>
    Friend Module AclFactory

        Friend Async Function GetAclItemAsync(context As DavContext, path As String) As Task(Of IHierarchyItemAsync)
            If path.Equals(AclFolder.AclFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Return New AclFolder(context)
            End If

            If path.Equals(UsersFolder.UsersFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Return New UsersFolder(context)
            End If

            If path.StartsWith(UsersFolder.UsersFolderPath.Trim("/"c), System.StringComparison.InvariantCultureIgnoreCase) Then
                Dim segments As String() = path.Split({"/"c}, StringSplitOptions.RemoveEmptyEntries)
                Dim userId As String = EncodeUtil.DecodeUrlPart(segments.Last())
                Return Await User.GetUserAsync(context, userId)
            End If

            Return Nothing
        End Function
    End Module
End Namespace
