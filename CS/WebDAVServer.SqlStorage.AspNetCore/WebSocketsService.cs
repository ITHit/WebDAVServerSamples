using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
namespace WebDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Notifies client about changes in WebDAV items.
    /// </summary>
    public class WebSocketsService
    {

        /// <summary>
        /// Dictionary which contains connected clients.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, WebSocketClient> clients = new ConcurrentDictionary<Guid, WebSocketClient>();

        /// <summary>
        /// Adds client to connected clients dictionary.
        /// </summary>
        /// <param name="client">Current client.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns>Client guid in the dictionary.</returns>
        public Guid AddClient(WebSocket client, string clientId)
        {
            Guid InternalClientId = Guid.NewGuid();
            clients.TryAdd(InternalClientId, new WebSocketClient { ClientID = clientId, Socket = client });
            return InternalClientId;
        }

        /// <summary>
        /// Removes client from connected clients dictionary.
        /// </summary>
        /// <param name="clientId">Client guid in the dictionary.</param>
        public void RemoveClient(Guid clientId)
        {
            WebSocketClient client;
            clients.TryRemove(clientId, out client);
        }

        /// <summary>
        /// Notifies client that file/folder was created.
        /// </summary>
        /// <param name="itemPath">file/folder.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task NotifyCreatedAsync(string itemPath, string clientId)
        {
            await SendMessage(itemPath, "created", clientId);
        }

        /// <summary>
        /// Notifies client that file/folder was updated.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task NotifyUpdatedAsync(string itemPath, string clientId)
        {
            await SendMessage(itemPath, "updated", clientId);
        }

        /// <summary>
        /// Notifies client that file/folder was deleted.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task NotifyDeletedAsync(string itemPath, string clientId)
        {
            await SendMessage(itemPath, "deleted", clientId);
        }

        /// <summary>
        /// Notifies client that file/folder was locked.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task NotifyLockedAsync(string itemPath, string clientId)
        {
            await SendMessage(itemPath, "locked", clientId);
        }

        /// <summary>
        /// Notifies client that file/folder was unlocked.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task NotifyUnLockedAsync(string itemPath, string clientId)
        {
            await SendMessage(itemPath, "unlocked", clientId);
        }

        /// <summary>
        /// Notifies client that file/folder was moved.
        /// </summary>
        /// <param name="itemPath">old file/folder path.</param>
        /// <param name="targetPath">new file/folder path.</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task NotifyMovedAsync(string itemPath, string targetPath, string clientId)
        {
            itemPath = itemPath.Trim('/');
            MovedNotification notifyObject = new MovedNotification
            {
                ItemPath = itemPath,
                TargetPath = targetPath,
                EventType = "moved"
            };
            foreach (WebSocketClient client in !string.IsNullOrEmpty(clientId) ? clients.Values.Where(p => p.ClientID != clientId) : clients.Values)
            {
                if (client.Socket.State == WebSocketState.Open)
                {
                    
                    await client.Socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notifyObject))), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Sends message about file/folder operation.
        /// </summary>
        /// <param name="itemPath">File/Folder path.</param>
        /// <param name="operation">Operation name: created/updated/deleted/moved</param>
        /// <param name="clientId">Current client Id.</param>
        /// <returns></returns>
        public async Task SendMessage(string itemPath, string operation, string clientId)
        {
            itemPath = itemPath.Trim('/');
            Notification notifyObject = new Notification
            {
                ItemPath = itemPath,
                EventType = operation
            };
            foreach (WebSocketClient client in !string.IsNullOrEmpty(clientId) ? clients.Values.Where(p => p.ClientID != clientId) : clients.Values)
            {
                if (client.Socket.State == WebSocketState.Open)
                {
                    await client.Socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notifyObject))), WebSocketMessageType.Text, true, CancellationToken.None);
                    
                }
            }
        }
    }

    /// <summary>
    /// Holds notification information.
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// Represents file/folder path.
        /// </summary>
        public string ItemPath { get; set; } = string.Empty;

        /// <summary>
        /// Represents event type.
        /// </summary>
        public string EventType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Holds notification moved information.
    /// </summary>
    public class MovedNotification : Notification
    {
        /// <summary>
        /// Represents target file/folder path.
        /// </summary>
        public string TargetPath { get; set; } = string.Empty;
    }

    /// <summary>
    /// Holds web socket data.
    /// </summary>
    public class WebSocketClient
    {
        /// <summary>
        /// Client ID.
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// Web Socket connector.
        /// </summary>
        public WebSocket Socket { get; set; }
    }
}
