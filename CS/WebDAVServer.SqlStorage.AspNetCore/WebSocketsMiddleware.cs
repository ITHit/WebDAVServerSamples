using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Threading;
using System.Text;
using Newtonsoft.Json;

namespace WebDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Middleware which implements notifications to clients using web sockets.
    /// Used to refresh files list when files or folders are created, updated, deleted, copied, moved, locked, etc.
    /// For the sake of simplicity this code does not work on a web farm. 
    /// In case of a web farm you must track notifications in your back-end storage on each web server and send
    /// notifications to all clients connected to that web server.
    /// </summary>
    public class WebSocketsMiddleware
    {
        /// <summary>
        /// Next middleware instance.
        /// </summary>
        private readonly RequestDelegate next;

        /// <summary>
        /// Singleton instance of <see cref="WebSocketsService"/>.
        /// </summary>
        private readonly WebSocketsService socketService;

        /// <summary>
        /// Initializes new instance of this class.
        /// </summary>
        /// <param name="next">Next middleware instance.</param>
        /// <param name="socketService">Singleton instance of <see cref="WebSocketsService"/>.</param>
        public WebSocketsMiddleware(RequestDelegate next, WebSocketsService socketService)
        {
            this.next = next;
            this.socketService = socketService;
        }

        public async Task Invoke(HttpContext context)
        {
            if(context.WebSockets.IsWebSocketRequest)
            {
                // If current request is web socket request.
                WebSocket client = await context.WebSockets.AcceptWebSocketAsync();
                // Adding client to connected clients dictionary.
                Guid clientId = socketService.AddClient(client);

                byte[] buffer = new byte[1024 * 4];

                while (client.State == WebSocketState.Open)
                {
                    try
                    {
                        // Must receive client results.
                        WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await client.CloseAsync(result.CloseStatus ?? WebSocketCloseStatus.ProtocolError, result.CloseStatusDescription, CancellationToken.None);
                        }
                    }
                    catch (WebSocketException)
                    {
                        break;
                    }
                }

                // Remove client from connected clients dictionary after disconnecting.
                socketService.RemoveClient(clientId);
            }
            else
            {
                // If not - invoke next middleware.
                await next(context);
            }
        }
    }

    /// <summary>
    /// Submits notifications to clients when any item on a WebDAV server is modified using web sockets.
    /// </summary>
    public static class WebSocketsMiddlewareExtensions
    {
        /// <summary>
        /// Adds middleware that submits notifications to clients when any item on a WebDAV server is modified using web sockets.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseWebSocketsMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<WebSocketsMiddleware>();
        }
    }
}
