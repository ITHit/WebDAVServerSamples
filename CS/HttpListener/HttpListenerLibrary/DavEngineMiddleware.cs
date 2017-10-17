using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ITHit.WebDAV.Server;
using HttpListenerLibrary.Options;

namespace HttpListenerLibrary
{
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
        /// <param name="env">The <see cref="IHostingEnvironment"/> instance.</param>
        public static void AddWebDav(this IServiceCollection services, IConfigurationRoot configuration, IHostingEnvironment env)
        {
            Configuration = configuration;

            services.AddSingleton<ILogger, ApplicationViewLogger>();
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddTransient<DavContextHttpListenerBaseAsync, DavContext>();
            services.Configure<DavEngineOptions>(async options => await Configuration.GetSection("DavEngineOptions").ReadOptionsAsync(options));
            services.Configure<DavContextOptions>(async options => await Configuration.GetSection("DavContextOptions").ReadOptionsAsync(options, env));
            services.Configure<DavLoggerOptions>(async options => await Configuration.GetSection("DavLoggerOptions").ReadOptionsAsync(options, env));
        }
    }
}

