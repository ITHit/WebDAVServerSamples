using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Config;
using ITHit.Server;
using ITHit.WebDAV.Server;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Configuration;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// An ASP.NET Core middleware that processes WebDAV requests.
    /// </summary>
    public class DavEngineMiddleware
    {
        /// <summary>
        /// WebDAV Engine instance.
        /// </summary>
        private readonly DavEngineCore engine;

        /// <summary>
        /// Initializes new instance of this class based on the WebDAV Engine instance.
        /// </summary>     
        /// <param name="next">Next middleware instance.</param>
        /// <param name="davEngineCore">WebDAV Engine instance.</param>
        public DavEngineMiddleware(RequestDelegate next, DavEngineCore davEngineCore)
        {
            this.engine = davEngineCore;
        }

        /// <summary>
        /// Processes WebDAV request.
        /// </summary>
        public async Task Invoke(HttpContext context, ContextCoreAsync<IHierarchyItemAsync> davContext, IOptions<DavContextConfig> config, ILogger logger)
        {
            if (context.Request.Method == "PUT")
            {
                // To enable file upload > 2Gb in case you are running .NET Core server in IIS:
                // 1. Unlock RequestFilteringModule on server level in IIS.
                // 2. Remove RequestFilteringModule on site level. Uncomment code in web.config to remove the module.
                // 3. Set MaxRequestBodySize = null.
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            }

            var watch = Stopwatch.StartNew();
            Trace.TraceWarning("All Engine" + context.Request.Method);
            await engine.RunAsync(davContext);
            watch.Stop();
            Trace.TraceWarning("All Engine run for " + context.Request.Method + ": " + watch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Extension methods to add WebDAV Engine capabilities to an HTTP application pipeline.
    /// </summary>
    public static class DavEngineMiddlewareExtensions
    {
        private static IConfiguration Configuration;

        /// <summary>
        /// Adds a WebDAV services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">The <see cref="IConfigurationRoot"/> instance.</param>
        /// <param name="env">The <see cref="IWebHostEnvironment"/> instance.</param>
        public static void AddWebDav(this IServiceCollection services, IConfigurationRoot configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            services.AddSingleton<DavEngineCore>();
            services.AddSingleton<ILogger, DavLoggerCore>();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddScoped<ContextCoreAsync<IHierarchyItemAsync>, DavContext>();
            services.Configure<DavEngineConfig>(async config => await Configuration.GetSection("WebDAVEngine").ReadConfigurationAsync(config));
            services.Configure<DavContextConfig>(async config => await Configuration.GetSection("Context").ReadConfigurationAsync(config, env));
            services.Configure<DavLoggerConfig>(async config => await Configuration.GetSection("Logger").ReadConfigurationAsync(config, env));
        }

        /// <summary>
        /// Adds a WebDAV Engine middleware type to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance. </returns>
        public static IApplicationBuilder UseWebDav(this IApplicationBuilder builder, IWebHostEnvironment env)
        {
            return builder.UseMiddleware<DavEngineMiddleware>();
        }
    }
}

