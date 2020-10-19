using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;
using CardDAVServer.SqlStorage.AspNetCore.CardDav;

namespace CardDAVServer.SqlStorage.AspNetCore
{
    public class ProvisioningMiddleware
    {
        /// <summary>
        /// Next middleware instance.
        /// </summary>
        private readonly RequestDelegate next;

        /// <summary>
        /// Initializes new instance of this class.
        /// </summary>
        /// <param name="next">Next middleware instance.</param>
        public ProvisioningMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Processes Provisioning request.
        /// </summary>
        public async Task Invoke(HttpContext context, ContextCoreAsync<IHierarchyItemAsync> davContext)
        {
            if (context.User != null && context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                await Provisioning.CreateAddressbookFoldersAsync(davContext as DavContext);
            }

            await next.Invoke(context);
        }
    }

    /// <summary>
    /// Extension methods to add Provisioning capabilities to an HTTP application pipeline.
    /// </summary>
    public static class ProvisioningeMiddlewareExtensions
    {
        /// <summary>
        /// Adds a capabilities middleware type to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        public static IApplicationBuilder UseProvisioninge(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ProvisioningMiddleware>();
        }
    }
}
