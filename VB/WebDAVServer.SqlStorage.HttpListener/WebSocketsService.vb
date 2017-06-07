Imports System
Imports System.Collections.Concurrent
Imports System.Net.WebSockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports Newtonsoft.Json

''' <summary>
''' Notifies client about changes in WebDAV items.
''' </summary>
Public Class WebSocketsService

    ''' <summary>
    ''' Singleton instance of <see cref="WebSocketsService"/> .
    ''' </summary>
    Public Shared ReadOnly Service As WebSocketsService = New WebSocketsService()

    ''' <summary>
    ''' Creates instance of this class.
    ''' </summary>
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Dictionary which contains connected clients.
    ''' </summary>
    Private ReadOnly clients As ConcurrentDictionary(Of Guid, WebSocket) = New ConcurrentDictionary(Of Guid, WebSocket)()

    Public Function AddClient(client As WebSocket) As Guid
        Dim clientId As Guid = Guid.NewGuid()
        clients.TryAdd(clientId, client)
        Return clientId
    End Function

    ''' <summary>
    ''' Removes client from connected clients dictionary.
    ''' </summary>
    ''' <param name="clientId">Client guid in the dictionary.</param>
    Public Sub RemoveClient(clientId As Guid)
        Dim client As WebSocket
        clients.TryRemove(clientId, client)
    End Sub

    Public Async Function NotifyRefreshAsync(folderPath As String) As Task
        folderPath = folderPath.TrimStart("/"c).TrimEnd("/"c)
        Dim notifyObject As Notification = New Notification With {.FolderPath = folderPath, .EventType = "refresh"}
        For Each client As WebSocket In clients.Values
            If client.State = WebSocketState.Open Then
                Await client.SendAsync(New ArraySegment(Of Byte)(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notifyObject))), WebSocketMessageType.Text, True, CancellationToken.None)
            End If
        Next
    End Function

    Public Async Function NotifyDeleteAsync(folderPath As String) As Task
        folderPath = folderPath.TrimStart("/"c).TrimEnd("/"c)
        Dim notifyObject As Notification = New Notification With {.FolderPath = folderPath, .EventType = "delete"}
        For Each client As WebSocket In clients.Values
            If client.State = WebSocketState.Open Then
                Await client.SendAsync(New ArraySegment(Of Byte)(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notifyObject))), WebSocketMessageType.Text, True, CancellationToken.None)
            End If
        Next
    End Function
End Class

''' <summary>
''' Holds notification information.
''' </summary>
Public Class Notification

    ''' <summary>
    ''' Represents notification data.
    ''' </summary>
    Public Property FolderPath As String

    ''' <summary>
    ''' Represents event type.
    ''' </summary>
    Public Property EventType As String
End Class
