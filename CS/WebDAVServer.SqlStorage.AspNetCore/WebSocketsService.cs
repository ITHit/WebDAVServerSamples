using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<Guid, WebSocket> clients = new ConcurrentDictionary<Guid, WebSocket>();

        /// <summary>
        /// Adds client to connected clients dictionary.
        /// </summary>
        /// <param name="client">Current client.</param>
        /// <returns>Client guid in the dictionary.</returns>
        public Guid AddClient(WebSocket client)
        {
            Guid clientId = Guid.NewGuid();
            clients.TryAdd(clientId, client);
            return clientId;
        }

        /// <summary>
        /// Removes client from connected clients dictionary.
        /// </summary>
        /// <param name="clientId">Client guid in the dictionary.</param>
        public void RemoveClient(Guid clientId)
        {
            WebSocket client;
            clients.TryRemove(clientId, out client);
        }

        /// <summary>
        /// Notifies client that file/folder was created.
        /// </summary>
        /// <param name="itemPath">file/folder.</param>
        /// <returns></returns>
        public async Task NotifyCreatedAsync(string itemPath)
        {
            await SendMessage(itemPath, "created");
        }

        /// <summary>
        /// Notifies client that file/folder was updated.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <returns></returns>
        public async Task NotifyUpdatedAsync(string itemPath)
        {
            await SendMessage(itemPath, "updated");
        }

        /// <summary>
        /// Notifies client that file/folder was deleted.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <returns></returns>
        public async Task NotifyDeletedAsync(string itemPath)
        {
            await SendMessage(itemPath, "deleted");
        }

        /// <summary>
        /// Notifies client that file/folder was locked.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <returns></returns>
        public async Task NotifyLockedAsync(string itemPath)
        {
            await SendMessage(itemPath, "locked");
        }

        /// <summary>
        /// Notifies client that file/folder was unlocked.
        /// </summary>
        /// <param name="itemPath">file/folder path.</param>
        /// <returns></returns>
        public async Task NotifyUnLockedAsync(string itemPath)
        {
            await SendMessage(itemPath, "unlocked");
        }

        /// <summary>
        /// Notifies client that file/folder was moved.
        /// </summary>
        /// <param name="itemPath">old file/folder path.</param>
        /// <param name="targetPath">new file/folder path.</param>
        /// <returns></returns>
        public async Task NotifyMovedAsync(string itemPath, string targetPath)
        {
            itemPath = itemPath.Trim('/');
            targetPath = targetPath.Trim('/');
            MovedNotification notifyObject = new MovedNotification
            {
                ItemPath = itemPath,
                TargetPath = targetPath,
                EventType = "moved"
            };
            foreach (WebSocket client in clients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    
                    await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notifyObject))), WebSocketMessageType.Text, true, CancellationToken.None);

                }
            }
        }

        /// <summary>
        /// Sends message about file/folder operation.
        /// </summary>
        /// <param name="itemPath">File/Folder path.</param>
        /// <param name="operation">Operation name: created/updated/deleted/moved</param>
        /// <returns></returns>
        public async Task SendMessage(string itemPath, string operation)
        {
            itemPath = itemPath.Trim('/');
            Notification notifyObject = new Notification
            {
                ItemPath = itemPath,
                EventType = operation
            };
            foreach (WebSocket client in clients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notifyObject))), WebSocketMessageType.Text, true, CancellationToken.None);
                    
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
}
