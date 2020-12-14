using System;
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace CalDAVServer.FileSystemStorage.AspNetCore.MSOFBAuthentication
{
    /// <summary>
    /// Implements MS-OFBA Authentication Middleware.
    /// </summary>
    /// <remarks>
    /// MS-OFBA enables log-in using third party login providers that require to present 
    /// HTML log-in page, such as Facebook, Twitter, Google, etc.
    /// 
    /// MS-OFBA is supported by Microsoft Office 2007 SP1 and later versions.
    /// MS-OFBA is not supported by Microsoft Mini-redirector and OS X Finder.
    /// </remarks>
    public class MSOFBAuthenticationMiddleware
    {
        private readonly RequestDelegate nextMiddleware;
        IOptions<MSOFBAuthenticationConfig> configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MSOFBAuthenticationMiddleware"/> class.
        /// </summary>
        /// <param name="next">The next.</param>
        /// <param name="config">The options.</param>
        public MSOFBAuthenticationMiddleware(RequestDelegate next, IOptions<MSOFBAuthenticationConfig> config)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }
            configuration = config;
            nextMiddleware = next;
        }
        /// <summary>
        /// Gets absolute URI from relative.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="relativeUrl">The relative URL.</param>
        private static string ToAbsolute(HttpRequest request, PathString relativeUrl)
        {
            return string.Format("{0}://{1}{2}", request.Scheme, request.Host.ToUriComponent(), relativeUrl.Value);
        }

        /// <summary>
        /// Handles the request.
        /// </summary>
        /// <param name="context">The request context.</param>        
        public async Task Invoke(HttpContext context)
        {
            // If it's non office request than we should not perform MS-OFBA logic.
            // We can't check it in AuthenticationHandler because base AuthenticationMiddleware
            // add's some logic and office web browser window works incorrectly.
            if (IsOFBAAccepted(context.Request) && !IsUserAuthenticated(context))
            {
                string redirectLocation = context.Response.Headers["Location"];

                string loginUri = ToAbsolute(context.Request, configuration.Value.LoginPath);
                string successUri = ToAbsolute(context.Request, configuration.Value.LoginSuccessPath);

                context.Response.StatusCode = 403;
                context.Response.Headers.Add("X-FORMS_BASED_AUTH_REQUIRED", new[] {
                    string.Format("{0}?{1}={2}", loginUri, configuration.Value.ReturnUrlParameter, configuration.Value.LoginSuccessPath)
                });
                context.Response.Headers.Add("X-FORMS_BASED_AUTH_RETURN_URL", new[] { successUri });
                context.Response.Headers.Add("X-FORMS_BASED_AUTH_DIALOG_SIZE", new[] { string.Format("{0}x{1}", 800, 600) });

            }
            else
            {
                await nextMiddleware(context);
            }
        }
        private bool IsUserAuthenticated(HttpContext Context)
            => Context.User != null && Context.User.Identity.IsAuthenticated;

        /// <summary>
        /// Analyzes request headers to determine MS-OFBA support.
        /// </summary>
        /// <remarks>
        /// MS-OFBA is supported by Microsoft Office 2007 SP1 and later versions 
        /// and any application that provides X-FORMS_BASED_AUTH_ACCEPTED: t header 
        /// in OPTIONS request.
        /// </remarks>
        private bool IsOFBAAccepted(HttpRequest Request)
        {
            StringValues ofbaAccepted = Request.Headers["X-FORMS_BASED_AUTH_ACCEPTED"];

            if (string.Equals(ofbaAccepted, "T", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            StringValues userAgent = Request.Headers["User-Agent"];

            if (userAgent.Count >= 1 && userAgent[0].Contains("Microsoft Office"))
            {
                return true;
            }

            return false;
        }
    }
}