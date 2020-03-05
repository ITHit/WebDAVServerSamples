using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.GSuite.Server;

using WebDAVServer.SqlStorage.AspNetCore.Options;

namespace WebDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// An ASP.NET Core middleware that processes GSuite requests.
    /// </summary>
    public class GSuiteEngineMiddleware
    {
        /// <summary>
        /// Next middleware instance.
        /// </summary>
        private readonly RequestDelegate next;

        /// <summary>
        /// GSuite Engine instance.
        /// </summary>
        private readonly GSuiteEngineCore engine;

        /// <summary>
        /// Initializes new instance of this class based on the GSuite Engine instance.
        /// </summary>     
        /// <param name="next">Next middleware instance.</param>
        /// <param name="GSuiteEngineCore">GSuite Engine instance.</param>
        public GSuiteEngineMiddleware(RequestDelegate next, GSuiteEngineCore engineCore)
        {
            this.engine = engineCore;
            this.next = next;
        }

        /// <summary>
        /// Processes GSuite request.
        /// </summary>
        public async Task Invoke(HttpContext context, ContextCoreAsync<IHierarchyItemAsync> davContext)
        {
            await engine.RunAsync(ContextConverter.ConvertToGSuiteContext(davContext));
            await next(context);
        }
    }

    /// <summary>
    /// Extension methods to add GSuite Engine capabilities to an HTTP application pipeline.
    /// </summary>
    public static class GSuiteEngineMiddlewareExtensions
    {
        private static IConfiguration Configuration;

        /// <summary>
        /// Adds a GSuite services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">The <see cref="IConfigurationRoot"/> instance.</param>
        public static void AddGSuite(this IServiceCollection services, IConfigurationRoot configuration)
        {
            Configuration = configuration;
            services.AddSingleton<GSuiteEngineCore>();

            services.Configure<GSuiteEngineOptions>(options => Configuration.GetSection("GSuiteEngineOptions").Bind(options));           
        }

        /// <summary>
        /// Adds a GSuite Engine middleware type to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance. </returns>
        public static IApplicationBuilder UseGSuite(this IApplicationBuilder builder)
        {
            // adds a GSuite Engine middleware only if GoogleServiceAccountID and GoogleServicePrivateKey are not empty in config file.
            var options = builder.ApplicationServices.GetService<IOptions<GSuiteEngineOptions>>();
            if (!string.IsNullOrEmpty(options.Value.GoogleServiceAccountID) && !string.IsNullOrEmpty(options.Value.GoogleServicePrivateKey))
            {
                builder.UseMiddleware<GSuiteEngineMiddleware>();
            }

            return builder;
        }
    }
}

