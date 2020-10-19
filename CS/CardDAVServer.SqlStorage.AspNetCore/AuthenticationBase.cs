using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CardDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Base class for challenge/response authentication ASP.NET Core middleware, ASP.NET modules, like Digest, Basic.
    /// </summary>
    public abstract class AuthenticationBase
    {
        /// <summary>
        /// Application realm.
        /// </summary>
        protected string realm = "ITHitWebDAVServer";

        /// <summary>
        /// Dictionary, which represents users credentials from storage in json settings.
        /// </summary>
        protected Dictionary<string, string> UserCollection;

        /// <summary>
        /// Fills UserCollection with values from storage.
        /// </summary>
        /// <param name="options">Users Options.</param>
        protected AuthenticationBase(IOptions<DavUsersConfig> config)
        {
            UserCollection = new Dictionary<string, string>();
            foreach (DavUser user in config.Value.Users)
            {
                if (!UserCollection.ContainsKey(user.UserName))
                    UserCollection.Add(user.UserName, user.Password);
            }
        }

        /// <summary>
        /// Setting unauthorized response for context.   
        /// </summary>
        /// <param name="context">Current Http context.</param>
        protected void Unauthorized(HttpContext context)
        {
            context.Response.OnStarting(SetAuthenticationHeader, context);
            context.Response.StatusCode = 401;
            context.Response.WriteAsync("401 Unauthorized");
        }

        /// <summary>
        /// Checks whether authorization header is present.
        /// </summary>
        /// <param name="request">Instance of <see cref="HttpRequest"/>.</param>
        /// <returns>'true' if there's authentication header with current authentication provider.</returns>
        protected bool IsAuthorizationPresent(HttpRequest request)
        {
            string authHeader = request.Headers[HeaderNames.Authorization];
            return authHeader != null && authHeader.StartsWith(AuthenicationProvider, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Represents name of authentication protocol.
        /// </summary>
        protected abstract string AuthenicationProvider { get; }

        /// <summary>
        /// Performs request authentication.
        /// </summary>
        /// <param name="request">Instance of <see cref="HttpRequest"/>.</param>
        /// <returns>Instance of <see cref="ClaimsPrincipal"/>, or <c>null</c> if user was not authenticated.</returns>
        protected abstract ClaimsPrincipal AuthenticateRequest(HttpRequest request);

        /// <summary>
        /// Sets authentication header to request authentication and show login dialog.
        /// </summary>
        /// <param name="context">Instance of current context.</param>
        /// <returns>Successfull task result.</returns>
        protected abstract Task SetAuthenticationHeader(object context);
    }
}
