Imports System
Imports System.Configuration
Imports System.IO
Imports System.Web
Imports System.Security.Principal
Imports System.Security.AccessControl
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server.Acl
Imports CalDAVServer.SqlStorage.AspNet.CalDav

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
            ' Create calendars for the user during first log-in.
            Await CreateCalendarFoldersAsync(context)
            ' Closes transaction. Calls DavContextBaseAsync.BeforeResponseAsync only first time this method is invoked.
            ' This method must be called manually if DavContextBaseAsync is used outside of DavEngine. 
            Await context.EnsureBeforeResponseWasCalledAsync()
        End Using
    End Function

    ''' <summary>
    ''' Creates initial calendars for users.
    ''' </summary>
    Friend Shared Async Function CreateCalendarFoldersAsync(context As DavContext) As Task
        ' If user does not have access to any calendars - create new calendars.
        Dim sql As String = "SELECT ISNULL((SELECT TOP 1 1 FROM [cal_Access] WHERE [UserId] = @UserId) , 0)"
        If Await context.ExecuteScalarAsync(Of Integer)(sql, "@UserId", context.UserId) < 1 Then
            Await CalendarFolder.CreateCalendarFolderAsync(context, "Cal 1", "Calendar 1")
            Await CalendarFolder.CreateCalendarFolderAsync(context, "Cal 2", "Calendar 2")
            Await CalendarFolder.CreateCalendarFolderAsync(context, "Cal 3", "Calendar 3")
        End If
    End Function
End Class
