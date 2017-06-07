Imports Microsoft.Web.WebSockets
Imports System

''' <summary>
''' Class for handling websocket requests.
''' </summary>
Public Class NotifyWebSocketsHandler
    Inherits WebSocketHandler

    ''' <summary>
    ''' Instance of service, which implements notifications and handling connections dictionary.
    ''' </summary>
    Private socketService As WebSocketsService

    ''' <summary>
    ''' Current client id.
    ''' </summary>
    Private clientId As Guid

    ''' <summary>
    ''' Initializes new instance of this class.
    ''' </summary>
    Public Sub New()
        ' Get singleton instance of service.
        socketService = WebSocketsService.Service
    End Sub

    ''' <summary>
    ''' Performs logic when socket connection is opened.
    ''' </summary>
    Public Overrides Sub OnOpen()
        ' Add current client to connected clients collection.
        clientId = socketService.AddClient(WebSocketContext.WebSocket)
    End Sub

    ''' <summary>
    ''' Performs logic when socket connection is being closed.
    ''' </summary>
    Public Overrides Sub OnClose()
        ' Remove client after connection was closed.
        socketService.RemoveClient(clientId)
    End Sub
End Class
