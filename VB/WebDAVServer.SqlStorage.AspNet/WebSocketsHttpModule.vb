Imports System
Imports System.Net.WebSockets
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web

''' <summary>
''' Http module which implements notifications to clients using web sockets.
''' Used to refresh files list when files or folders are created, updated, deleted, copied, moved, locked, etc.
''' For the sake of simplicity this code does not work on a web farm. 
''' In case of a web farm you must track notifications in your back-end storage on each web server and send
''' notifications to all clients connected to that web server.
''' </summary>
Public Class WebSocketsHttpModule
    Implements IHttpModule

    ''' <summary>
    ''' Instance of service, which implements notifications and handling connections dictionary.
    ''' </summary>
    Private socketService As WebSocketsService

    ''' <summary>
    ''' Initializes new instance of this class.
    ''' </summary>
    Public Sub New()
        socketService = WebSocketsService.Service
    End Sub

    Public Sub Dispose() Implements IHttpModule.Dispose
    End Sub

    Public Sub Init(context As HttpApplication) Implements IHttpModule.Init
        AddHandler context.AcquireRequestState, New EventHandler(AddressOf CheckState)
    End Sub

    ''' <summary>
    ''' Checks if current request is web socket request.
    ''' </summary>
    ''' <param name="sender">Event sender.</param>
    ''' <param name="e">Event arguments.</param>
    Private Sub CheckState(sender As Object, e As EventArgs)
        Dim context As HttpContext = CType(sender, HttpApplication).Context
        If context.IsWebSocketRequest Then
            ' Handle request if it is web socket request and end pipeline.
            context.AcceptWebSocketRequest(AddressOf HandleWebSocketRequest)
            context.ApplicationInstance.CompleteRequest()
        End If
    End Sub

    ''' <summary>
    ''' Handles websocket connection logic.
    ''' </summary>
    ''' <param name="webSocketContext">Instance of <see cref="WebSocketContext"/> .</param>
    Private Async Function HandleWebSocketRequest(webSocketContext As WebSocketContext) As Task
        Dim client As WebSocket = webSocketContext.WebSocket
        ' Adding client to connected clients dictionary.
        Dim clientId As Guid = socketService.AddClient(client)
        Dim buffer As Byte() = New Byte(4095) {}
        Dim result As WebSocketReceiveResult = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None)
        While Not result.CloseStatus.HasValue
            ' Must receive client results to update client state and detect disconnecting.
            result = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None)
        End While

        ' Remove client from connected clients dictionary after disconnecting.
        socketService.RemoveClient(clientId)
        Await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None)
    End Function
End Class
