using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace WebDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Http module which implements notifications to clients using web sockets.
    /// Used to refresh files list when files or folders are created, updated, deleted, copied, moved, locked, etc.
    /// For the sake of simplicity this code does not work on a web farm. 
    /// In case of a web farm you must track notifications in your back-end storage on each web server and send
    /// notifications to all clients connected to that web server.
    /// </summary>
    public class WebSocketsHttpModule : IHttpModule
    {
        /// <summary>
        /// Instance of service, which implements notifications and handling connections dictionary.
        /// </summary>
        private WebSocketsService socketService;

        /// <summary>
        /// Initializes new instance of this class.
        /// </summary>
        public WebSocketsHttpModule()
        {
            socketService = WebSocketsService.Service;
        }

        public void Dispose()
        {  }

        public void Init(HttpApplication context)
        {
            context.AcquireRequestState += new EventHandler(CheckState);
        }

        /// <summary>
        /// Checks if current request is web socket request.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CheckState(object sender, EventArgs e)
        {
            HttpContext context = ((HttpApplication)sender).Context;
            if(context.IsWebSocketRequest && context.Request.RawUrl.StartsWith("/dav"))
            {
                // Handle request if it is web socket request and end pipeline.
                context.AcceptWebSocketRequest(HandleWebSocketRequest);
                context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Handles websocket connection logic.
        /// </summary>
        /// <param name="webSocketContext">Instance of <see cref="WebSocketContext"/>.</param>
        private async Task HandleWebSocketRequest(WebSocketContext webSocketContext)
        {
            WebSocket client = webSocketContext.WebSocket;

            // Adding client to connected clients dictionary.
            Guid clientId = socketService.AddClient(client);

            byte[] buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = null;

            while (client.State == WebSocketState.Open)
            {
                try
                {
                    // Must receive client results.
                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }

            // Remove client from connected clients dictionary after disconnecting.
            socketService.RemoveClient(clientId);
        }
    }
}