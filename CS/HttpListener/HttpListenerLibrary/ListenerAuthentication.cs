using System;
using System.Net;
using System.Security.Principal;
using System.Text;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Performs authentication logic for HttpListener.
    /// </summary>
    public class ListenerAuthentication
    {
        /// <summary>
        /// Authentication provider instance.
        /// </summary>
        IAuthenticationProvider authenticationProvider;

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="authenticationProvider">Specified authentication provider.</param>
        public ListenerAuthentication(IAuthenticationProvider authenticationProvider)
        {
            this.authenticationProvider = authenticationProvider;
        }

        /// <summary>
        /// Performs authentication.
        /// </summary>
        /// <param name="context">Current HttpListener context.</param>
        /// <returns>Authenticated <see cref="IPrincipal"/> instance or <c>null</c> otherwise.</returns>
        public IPrincipal PerformAuthentication(HttpListenerContext context)
        {
            IPrincipal principal = AuthenticateRequest(context.Request);

            // OPTIONS request must be processed without authentication. 
            // Some clients including web browser preflight requests and Microsoft Mini-redirector 
            // does not attach Authorization header to OPTIONS request but require correct 
            // response to such request.
            if (!context.Request.HttpMethod.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase))
            {
                if (principal == null)
                {
                    context.Response.StatusCode = 401;
                    ShowLoginDialog(context);
                    return null;
                }
            }
            else
            {
                if (principal == null)
                {
                    principal = new GenericPrincipal(new GenericIdentity(""), null); // Empty user name sets IsAuthenticated = false
                }
            }
            return principal;
        }

        /// <summary>
        /// Adds authentication header to the response and forces web browser to get user credentials for authentication.
        /// </summary>
        /// <param name="context"><see cref="HttpListenerContext"/> instance.</param>
        private void ShowLoginDialog(HttpListenerContext context)
        {
            context.Response.AddHeader("WWW-Authenticate", authenticationProvider.GetChallenge());

            if (!context.Request.HttpMethod.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase)
                && !context.Request.HttpMethod.Equals("HEAD", StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] message = new UTF8Encoding().GetBytes("Access is denied.");
                context.Response.ContentLength64 = message.Length;
                context.Response.OutputStream.Write(message, 0, message.Length);
            }
        }

        /// <summary>
        /// Authenticates current request.
        /// </summary>
        /// <param name="request"><see cref="HttpListenerRequest"/> instance.</param>
        /// <returns>Authenticated <see cref="IPrincipal"/> instance.</returns>
        private IPrincipal AuthenticateRequest(HttpListenerRequest request)
        {
            return authenticationProvider.AuthenticateRequest(request.Headers, request.HttpMethod);
        }
    }
}
