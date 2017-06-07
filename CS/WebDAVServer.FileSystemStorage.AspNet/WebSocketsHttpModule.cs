using System;
using System.Web;
using Microsoft.Web.WebSockets;

namespace WebDAVServer.FileSystemStorage.AspNet
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
        public void Dispose()
        {  }

        public void Init(HttpApplication context)
        {
            context.AcquireRequestState += new EventHandler(CheckState);
        }

        private void CheckState(object sender, EventArgs e)
        {
            HttpContext context = ((HttpApplication)sender).Context;
            if(context.IsWebSocketRequest)
            {
                // Handle request if it is web socket request and end pipeline.
                context.AcceptWebSocketRequest(new NotifyWebSocketsHandler());
                context.ApplicationInstance.CompleteRequest();
            }
        }
    }
}