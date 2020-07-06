using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;


namespace AzureDataLakeStorage
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
            clients.TryRemove(clientId, out _);
        }

        /// <summary>
        /// Notifies client that content in the specified folder has been changed. 
        /// Called when one of the following events occurs in the specified folder: file or folder created, file or folder updated, file deleted.
        /// </summary>
        /// <param name="folderPath">Content of this folder was modified.</param>
        /// <returns></returns>
        public async Task NotifyRefreshAsync(string folderPath)
        {
            folderPath = folderPath.Trim('/');
            Notification notifyObject = new Notification
            {
                FolderPath = folderPath,
                EventType = "refresh"
            };
            foreach (WebSocket client in clients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notifyObject))), WebSocketMessageType.Text, true, CancellationToken.None);
                }
            }
        }

        /// <summary>
        /// Notifies client that folder was deleted.
        /// </summary>
        /// <param name="folderPath">Folder that was deleted.</param>
        /// <returns></returns>
        public async Task NotifyDeleteAsync(string folderPath)
        {
            folderPath = folderPath.Trim('/');
            Notification notifyObject = new Notification
            {
                FolderPath = folderPath,
                EventType = "delete"
            };
            foreach (WebSocket client in clients.Values)
            {
                if (client.State == WebSocketState.Open)
                {
                    await client.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(notifyObject))), WebSocketMessageType.Text, true, CancellationToken.None);
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
        /// Represents notification data.
        /// </summary>
        public string FolderPath { get; set; } = string.Empty;

        /// <summary>
        /// Represents event type.
        /// </summary>
        public string EventType { get; set; } = string.Empty;
    }
}
