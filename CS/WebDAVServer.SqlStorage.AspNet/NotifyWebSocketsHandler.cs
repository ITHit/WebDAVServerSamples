using Microsoft.Web.WebSockets;
using System;

namespace WebDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Class for handling websocket requests.
    /// </summary>
    public class NotifyWebSocketsHandler : WebSocketHandler
    {
        /// <summary>
        /// Instance of service, which implements notifications and handling connections dictionary.
        /// </summary>
        private WebSocketsService socketService;

        /// <summary>
        /// Current client id.
        /// </summary>
        private Guid clientId;

        /// <summary>
        /// Initializes new instance of this class.
        /// </summary>
        public NotifyWebSocketsHandler()
        {
            // Get singleton instance of service.
            socketService = WebSocketsService.Service;
        }

        /// <summary>
        /// Performs logic when socket connection is opened.
        /// </summary>
        public override void OnOpen()
        {
            // Add current client to connected clients collection.
            clientId = socketService.AddClient(WebSocketContext.WebSocket);
        }

        /// <summary>
        /// Performs logic when socket connection is being closed.
        /// </summary>
        public override void OnClose()
        {
            // Remove client after connection was closed.
            socketService.RemoveClient(clientId);
        }
    }
}