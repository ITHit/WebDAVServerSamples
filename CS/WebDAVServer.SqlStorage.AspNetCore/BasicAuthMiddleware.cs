using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace WebDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Middleware which implements 'Basic' authentication protocol.
    /// </summary>
    public class BasicAuthMiddleware : AuthenticationBase
    {
        /// <summary>
        /// Next middleware instance.
        /// </summary>
        private readonly RequestDelegate next;

        /// <summary>
        /// Represents name of basic authentication protocol.
        /// </summary>
        protected override string AuthenicationProvider { get { return "Basic"; } }

        /// <summary>
        /// Initializes new instance of this class.
        /// </summary>
        /// <param name="next">Next middleware instance.</param>
        /// <param name="options">Users config.</param>
        public BasicAuthMiddleware(RequestDelegate next, IOptions<DavUsersConfig> config) : base(config)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // If Authorize header is present - perform request authenticating.
            if(IsAuthorizationPresent(context.Request))
            {
                ClaimsPrincipal userPrincipal = AuthenticateRequest(context.Request);
                if (userPrincipal.Identity != null)
                {
                    // Authenticated succesfully.
                    context.User = userPrincipal;
                    await next(context);
                }
                else
                {
                    // Invalid credentials.
                    Unauthorized(context);
                    return;
                }
            }
            else
            {
                // To support Miniredirector/Web Folders on XP and Server 2003 as well as 
                // Firefox CORS requests, OPTIONS must be processed without authorization.
                // MS Office for Mac requires OPTIONS request to be authenticated.
                if (context.Request.Method == "OPTIONS" &&
                    !(context.Request.Headers["User-Agent"].ToString().StartsWith("Microsoft Office")))
                {
                    await next(context);
                } 
                else
                {
                    Unauthorized(context);
                    return;
                }
            }
        }

        /// <summary>
        /// Performs request with basic authentication.
        /// </summary>
        /// <param name="request">Instance of <see cref="HttpRequest"/>.</param>
        /// <returns>Instance of <see cref="ClaimsPrincipal"/>, or <c>null</c> if user was not authenticated.</returns>
        protected override ClaimsPrincipal AuthenticateRequest(HttpRequest request)
        {
            // Getting authorize header string.
            string headerString = request.Headers[HeaderNames.Authorization].ToString();
            string encodedString = headerString.Substring(AuthenicationProvider.Length + 1).Trim();

            // Decode username and password.
            byte[] bytesCredentials = Convert.FromBase64String(encodedString);
            string[] credentials = new UTF8Encoding().GetString(bytesCredentials).Split(':');
            string userName = credentials[0];
            string password = credentials[1];

            // Windows Vista sends user name in the form DOMAIN\User.
            int delimiterIndex = userName.IndexOf('\\');
            if (delimiterIndex != -1)
            {
                userName = userName.Remove(0, delimiterIndex + 1);
            }

            // Check credentials in user storage.
            if (UserCollection.ContainsKey(userName))
            {
                if(password == UserCollection[userName])
                {
                    // Authenticated succesfully.
                    List<Claim> claims = new List<Claim>();
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, userName));
                    claims.Add(new Claim(ClaimTypes.Name, userName));
                    ClaimsIdentity identity = new ClaimsIdentity(claims, "Basic");
                    return new ClaimsPrincipal(identity);
                }
                else
                {
                    return new ClaimsPrincipal();
                }
            }
            else
            {
                return new ClaimsPrincipal();
            }
        }

        /// <summary>
        /// Sets authentication header to request basic authentication and show login dialog.
        /// </summary>
        /// <param name="context">Instance of current context.</param>
        /// <returns>Successfull task result.</returns>
        protected override Task SetAuthenticationHeader(object context)
        {
            HttpContext httpContext = (HttpContext)context;
            httpContext.Response.Headers.Append(HeaderNames.WWWAuthenticate, $"{AuthenicationProvider} realm=\"{realm}\"");
            return Task.FromResult(0);
        }
    }

    /// <summary>
    /// Class with Basic Authentication middleware extensions.
    /// </summary>
    public static class BasicAuthMiddlewareExtensions
    {
        /// <summary>
        /// Add Basic Authentication middleware.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseBasicAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BasicAuthMiddleware>();
        }
    }
}
