Imports System
Imports System.Collections.Concurrent
Imports System.Net.WebSockets
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks
Imports System.Web.Script.Serialization

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

    ''' <summary>
    ''' Adds client to connected clients dictionary.
    ''' </summary>
    ''' <param name="client">Current client.</param>
    ''' <returns>Client guid in the dictionary.</returns>
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

    ''' <summary>
    ''' Notifies client that file/folder was created.
    ''' </summary>
    ''' <param name="itemPath">file/folder.</param>
    ''' <returns></returns>
    Public Async Function NotifyCreatedAsync(itemPath As String) As Task
        Await SendMessage(itemPath, "created")
    End Function

    ''' <summary>
    ''' Notifies client that file/folder was updated.
    ''' </summary>
    ''' <param name="itemPath">file/folder path.</param>
    ''' <returns></returns>
    Public Async Function NotifyUpdatedAsync(itemPath As String) As Task
        Await SendMessage(itemPath, "updated")
    End Function

    ''' <summary>
    ''' Notifies client that file/folder was deleted.
    ''' </summary>
    ''' <param name="itemPath">file/folder path.</param>
    ''' <returns></returns>
    Public Async Function NotifyDeletedAsync(itemPath As String) As Task
        Await SendMessage(itemPath, "deleted")
    End Function

    ''' <summary>
    ''' Notifies client that file/folder was locked.
    ''' </summary>
    ''' <param name="itemPath">file/folder path.</param>
    ''' <returns></returns>
    Public Async Function NotifyLockedAsync(itemPath As String) As Task
        Await SendMessage(itemPath, "locked")
    End Function

    ''' <summary>
    ''' Notifies client that file/folder was unlocked.
    ''' </summary>
    ''' <param name="itemPath">file/folder path.</param>
    ''' <returns></returns>
    Public Async Function NotifyUnLockedAsync(itemPath As String) As Task
        Await SendMessage(itemPath, "unlocked")
    End Function

    ''' <summary>
    ''' Notifies client that file/folder was moved.
    ''' </summary>
    ''' <param name="itemPath">old file/folder path.</param>
    ''' <param name="targetPath">new file/folder path.</param>
    ''' <returns></returns>
    Public Async Function NotifyMovedAsync(itemPath As String, targetPath As String) As Task
        itemPath = itemPath.Trim("/"c)
        targetPath = targetPath.Trim("/"c)
        Dim notifyObject As MovedNotification = New MovedNotification With {.ItemPath = itemPath,
                                                                      .TargetPath = targetPath,
                                                                      .EventType = "moved"}
        For Each client As WebSocket In clients.Values
            If client.State = WebSocketState.Open Then
                Await client.SendAsync(New ArraySegment(Of Byte)(Encoding.UTF8.GetBytes(New JavaScriptSerializer().Serialize(notifyObject))), WebSocketMessageType.Text, True, CancellationToken.None)
            End If
        Next
    End Function

    ''' <summary>
    ''' Sends message about file/folder operation.
    ''' </summary>
    ''' <param name="itemPath">File/Folder path.</param>
    ''' <param name="operation">Operation name: created/updated/deleted/moved</param>
    ''' <returns></returns>
    Public Async Function SendMessage(itemPath As String, operation As String) As Task
        itemPath = itemPath.Trim("/"c)
        Dim notifyObject As Notification = New Notification With {.ItemPath = itemPath,
                                                            .EventType = operation
                                                            }
        For Each client As WebSocket In clients.Values
            If client.State = WebSocketState.Open Then
                Await client.SendAsync(New ArraySegment(Of Byte)(Encoding.UTF8.GetBytes(New JavaScriptSerializer().Serialize(notifyObject))), WebSocketMessageType.Text, True, CancellationToken.None)
            End If
        Next
    End Function
End Class

''' <summary>
''' Holds notification information.
''' </summary>
Public Class Notification

    ''' <summary>
    ''' Represents file/folder path.
    ''' </summary>
    Public Property ItemPath As String = String.Empty

    ''' <summary>
    ''' Represents event type.
    ''' </summary>
    Public Property EventType As String = String.Empty
End Class

''' <summary>
''' Holds notification moved information.
''' </summary>
Public Class MovedNotification
    Inherits Notification

    ''' <summary>
    ''' Represents target file/folder path.
    ''' </summary>
    Public Property TargetPath As String = String.Empty
End Class
