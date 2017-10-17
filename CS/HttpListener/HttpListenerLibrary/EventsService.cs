using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Notifies client about changes in WebDAV items.
    /// </summary>
    public class EventsService
    {
        /// <summary>
        /// Dictionary which contains connected clients.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, HttpListenerResponse> clients = new ConcurrentDictionary<Guid, HttpListenerResponse>();

        /// <summary>
        /// Adds client to connected clients dictionary.
        /// </summary>
        /// <param name="client">Current client.</param>
        /// <returns>Client guid in the dictionary.</returns>
        public Guid AddClient(HttpListenerResponse client)
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
            clients.TryRemove(clientId, out HttpListenerResponse client);
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
            byte[] buffer = Encoding.UTF8.GetBytes($"event: refresh\ndata: {folderPath}\n\n");
            foreach (KeyValuePair<Guid, HttpListenerResponse> client in clients)
            {
                try
                {
                    await client.Value.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    client.Value.OutputStream.Flush();
                }
                catch(IOException ex)
                {
                    if(ex.InnerException is SocketException)
                    {
                        // If client is disconnected
                        RemoveClient(client.Key);
                        client.Value.Close();
                    }
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
            byte[] buffer = Encoding.UTF8.GetBytes($"event: delete\ndata: {folderPath}\n\n");
            foreach (KeyValuePair<Guid, HttpListenerResponse> client in clients)
            {
                try
                {
                    await client.Value.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    client.Value.OutputStream.Flush();
                }
                catch (IOException ex)
                {
                    if (ex.InnerException is SocketException)
                    {
                        // If client is disconnected
                        RemoveClient(client.Key);
                        client.Value.Close();
                    }
                }
            }
        }
    }
}
