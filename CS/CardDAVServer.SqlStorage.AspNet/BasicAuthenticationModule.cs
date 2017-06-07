using System;
using System.Web;
using System.Text;
using System.Security.Principal;
using System.Security;
using System.Web.Security;

namespace CardDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// ASP.NET module which implements 'Basic' authentication protocol.
    /// </summary>
    public class BasicAuthenticationModule : AuthenticationModuleBase
    {
        /// <summary>
        /// Checks whether authorization header is present.
        /// </summary>
        /// <param name="request">Instance of <see cref="HttpRequest"/>.</param>
        /// <returns>'true' if there's basic authentication header.</returns>
        protected override bool IsAuthorizationPresent(HttpRequest request)
        {
            string auth = request.Headers["Authorization"];
            return auth != null && auth.Substring(0, 5).ToLower() == "basic";
        }

        /// <summary>
        /// Performs request authentication.
        /// </summary>
        /// <param name="request">Instance of <see cref="HttpRequest"/>.</param>
        /// <returns>Instance of <see cref="IPrincipal"/>, or <c>null</c> if user was not authenticated.</returns>
        protected override IPrincipal AuthenticateRequest(HttpRequest request)
        {
            string auth = request.Headers["Authorization"];
            // decode username and password
            string base64Credentials = auth.Substring(6);
            byte[] bytesCredentials = Convert.FromBase64String(base64Credentials);
            string[] credentials = new UTF8Encoding().GetString(bytesCredentials).Split(':');
            string userName = credentials[0];
            string password = credentials[1];

            // Windows Vista sends user name in the form DOMAIN\User
            int delimiterIndex = userName.IndexOf('\\');
            if (delimiterIndex != -1)
            {
                userName = userName.Remove(0, delimiterIndex + 1);
            }

            try
            {
                if (Membership.ValidateUser(userName, password))
                { // authenticated succesefully
                    return new GenericPrincipal(new GenericIdentity(userName), null);
                }
                else
                { // invalid credentials
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to authenticate user", ex);
                return null;
            }
        }

        /// <summary>
        /// Gets challenge string.
        /// </summary>
        /// <returns>Challenge string.</returns>
        protected override string GetChallenge()
        {
            return "Basic Realm=\"My WebDAV Server\"";
        }
    }

}
