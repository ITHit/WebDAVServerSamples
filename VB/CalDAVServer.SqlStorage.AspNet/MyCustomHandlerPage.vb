Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Web
Imports System.Web.UI
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.CalDav

Public Class MyCustomHandlerPage
    Inherits Page

    Protected Sub New()
        If Type.GetType("Mono.Runtime") Is Nothing Then
            AddHandler Me.Load, AddressOf Page_LoadAsync
        End If
    End Sub

    Private Sub Page_LoadAsync(sender As Object, e As EventArgs)
        RegisterAsyncTask(New PageAsyncTask(AddressOf GetPageDataAsync))
    End Sub

    Public Async Function GetPageDataAsync() As Task
        Using context As DavContext = New DavContext(HttpContext.Current)
            Dim discovery As Discovery = New Discovery(context)
            ' Get all user calendars Urls.
            ' Get list of folders that contain user calendars and enumerate calendars in each folder.
            For Each folder As IItemCollection In Await discovery.GetCalendarHomeSetAsync()
                Dim children As IEnumerable(Of IHierarchyItem) =(Await folder.GetChildrenAsync(New PropertyName(-1) {}, Nothing, Nothing, Nothing)).Page
                AllUserCalendars.AddRange(children.Where(Function(x) TypeOf x Is ICalendarFolder))
            Next
        End Using
    End Function

    ''' <summary>
    ''' Gets all user calendars.
    ''' </summary>
    Public AllUserCalendars As List(Of IHierarchyItem) = New List(Of IHierarchyItem)()

    Public Shared ReadOnly Property ApplicationPath As String
        Get
            Using context As DavContext = New DavContext(HttpContext.Current)
                Dim url As Uri = HttpContext.Current.Request.Url
                Dim server As String = url.Scheme & "://" & url.Host & (If(url.IsDefaultPort, "", ":" & url.Port.ToString())) & "/" & context.Request.ApplicationPath.Trim("/"c)
                Return server.TrimEnd("/"c) & "/"c
            End Using
        End Get
    End Property
End Class
