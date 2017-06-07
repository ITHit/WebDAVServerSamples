Imports System
Imports System.Web
Imports Microsoft.Web.WebSockets

''' <summary>
''' Http module which implements notifications to clients using web sockets.
''' Used to refresh files list when files or folders are created, updated, deleted, copied, moved, locked, etc.
''' For the sake of simplicity this code does not work on a web farm. 
''' In case of a web farm you must track notifications in your back-end storage on each web server and send
''' notifications to all clients connected to that web server.
''' </summary>
Public Class WebSocketsHttpModule
    Implements IHttpModule

    Public Sub Dispose() Implements IHttpModule.Dispose
    End Sub

    Public Sub Init(context As HttpApplication) Implements IHttpModule.Init
        AddHandler context.AcquireRequestState, New EventHandler(AddressOf CheckState)
    End Sub

    Private Sub CheckState(sender As Object, e As EventArgs)
        Dim context As HttpContext = CType(sender, HttpApplication).Context
        If context.IsWebSocketRequest Then
            ' Handle request if it is web socket request and end pipeline.
            context.AcceptWebSocketRequest(New NotifyWebSocketsHandler())
            context.ApplicationInstance.CompleteRequest()
        End If
    End Sub
End Class
