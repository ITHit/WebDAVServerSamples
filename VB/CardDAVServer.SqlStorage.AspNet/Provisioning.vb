Imports System
Imports System.Configuration
Imports System.IO
Imports System.Web
Imports System.Security.Principal
Imports System.Security.AccessControl
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server.Acl
Imports CardDAVServer.SqlStorage.AspNet.CardDav

''' <summary>
''' This class creates initial calendar(s) and address book(s) for user during first log-in.
''' </summary>
Public Class Provisioning
    Implements IHttpModule

    Public Sub Dispose() Implements IHttpModule.Dispose
    End Sub

    Public Sub Init(application As HttpApplication) Implements IHttpModule.Init
        Dim postAuthAsyncHelper As EventHandlerTaskAsyncHelper = New EventHandlerTaskAsyncHelper(AddressOf App_OnPostAuthenticateRequestAsync)
        application.AddOnPostAuthenticateRequestAsync(postAuthAsyncHelper.BeginEventHandler, postAuthAsyncHelper.EndEventHandler)
    End Sub

    Private Async Function App_OnPostAuthenticateRequestAsync(source As Object, eventArgs As EventArgs) As Task
        Dim httpContext As HttpContext = HttpContext.Current
        If(httpContext.User Is Nothing) OrElse Not httpContext.User.Identity.IsAuthenticated Then Return
        Using context As DavContext = New DavContext(httpContext)
            ' Create addressboks for the user during first log-in.
            Await CreateAddressbookFoldersAsync(context)
            ' Closes transaction. Calls ContextAsync{IHierarchyItemAsync}.BeforeResponseAsync only first time this method is invoked.
            ' This method must be called manually if ContextAsync{IHierarchyItemAsync} is used outside of DavEngine. 
            Await context.EnsureBeforeResponseWasCalledAsync()
        End Using
    End Function

    ''' <summary>
    ''' Creates initial address books for user.
    ''' </summary>
    Friend Shared Async Function CreateAddressbookFoldersAsync(context As DavContext) As Task
        ' If user does not have access to any address books - create new address books.
        Dim sql As String = "SELECT ISNULL((SELECT TOP 1 1 FROM [card_Access] WHERE [UserId] = @UserId) , 0)"
        If Await context.ExecuteScalarAsync(Of Integer)(sql, "@UserId", context.UserId) < 1 Then
            Await AddressbookFolder.CreateAddressbookFolderAsync(context, "Book 1", "Address Book 1")
            Await AddressbookFolder.CreateAddressbookFolderAsync(context, "Book 2", "Address Book 2")
            Await AddressbookFolder.CreateAddressbookFolderAsync(context, "Book 3", "Address Book 3")
        End If
    End Function
End Class
