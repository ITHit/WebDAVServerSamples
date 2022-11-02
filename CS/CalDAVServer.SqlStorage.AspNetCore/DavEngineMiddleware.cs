using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

using ITHit.Server;
using ILogger = ITHit.Server.ILogger;
using ITHit.WebDAV.Server;
using CalDAVServer.SqlStorage.AspNetCore.Configuration;

namespace CalDAVServer.SqlStorage.AspNetCore
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
        public DavEngineMiddleware(RequestDelegate next, DavEngineCore engineCore)
        {
            this.engine = engineCore;
        }

        /// <summary>
        /// Processes WebDAV request.
        /// </summary>
        public async Task Invoke(HttpContext context, ContextCoreAsync<IHierarchyItem> davContext, IOptions<DavContextConfig> config, ILogger logger)
        {
            if (context.Request.Method == "PUT")
            {
                // To enable file upload > 2Gb in case you are running .NET Core server in IIS:
                // 1. Unlock RequestFilteringModule on server level in IIS.
                // 2. Remove RequestFilteringModule on site level. Uncomment code in web.config to remove the module.
                // 3. Set MaxRequestBodySize = null.
                context.Features.Get<IHttpMaxRequestBodySizeFeature>().MaxRequestBodySize = null;
            }
            await engine.RunAsync(davContext);
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

            services.AddScoped<ContextCoreAsync<IHierarchyItem>, DavContext>();
            services.Configure<DavEngineConfig>(async config => await Configuration.GetSection("WebDAVEngine").ReadConfigurationAsync(config));
            services.Configure<DavContextConfig>(async config => await Configuration.GetSection("Context").ReadConfigurationAsync(config, env));
            services.Configure<DavLoggerConfig>(async config => await Configuration.GetSection("Logger").ReadConfigurationAsync(config, env));
            services.Configure<DavUsersConfig>(config => Configuration.GetSection("Users").Bind(config));
        }

        /// <summary>
        /// Adds a WebDAV Engine middleware type to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance. </returns>
        public static IApplicationBuilder UseWebDav(this IApplicationBuilder builder, IWebHostEnvironment env)
        {
            CreateDatabaseSchema(builder, env);
            return builder.UseMiddleware<DavEngineMiddleware>();
        }
        /// <summary>
        /// Creates database if it does not exist.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <param name="env">The <see cref="IApplicationBuilder"/> instance.</param>
        public static void CreateDatabaseSchema(IApplicationBuilder builder, IWebHostEnvironment env)
        {
            bool databaseExists = false;
            DavContextConfig contextConfig = builder.ApplicationServices.GetService<IOptions<DavContextConfig>>().Value;
            SqlConnectionStringBuilder sqlConnectionStringBuilder = new SqlConnectionStringBuilder(contextConfig.ConnectionString);
            // extracts initial catalog name
            string databaseName = sqlConnectionStringBuilder.InitialCatalog;
            // sets initial catalog to master 
            sqlConnectionStringBuilder.InitialCatalog = "master";
           
            using (SqlConnection sqlConnection = new SqlConnection(sqlConnectionStringBuilder.ConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand sqlCommand = new SqlCommand($"SELECT count(*) from dbo.sysdatabases where name = '{databaseName}'", sqlConnection))
                {
                    databaseExists = ((int)sqlCommand.ExecuteScalar() != 0);
                }

                if (!databaseExists)
                {
                    var scriptFi = new FileInfo(Path.Combine(env.ContentRootPath, "WebDAVServerImpl\\DB.sql"));
                    if (!scriptFi.Exists)
                        scriptFi = new FileInfo(Path.Combine(env.ContentRootPath, "DB.sql"));
                    if (scriptFi.Exists)
                        RunScript(sqlConnection, File.ReadAllText(scriptFi.FullName));
                }
            }
        }

        /// <summary>
        /// Executes sql script.
        /// </summary>
        /// <param name="connection">The <see cref="SqlConnection"/> instance.</param>
        /// <param name="sqlScript">sql script</param>
        private static void RunScript(SqlConnection connection, string sqlScript)
        {
            string[] commands = sqlScript.Split(new[] { "GO" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string command in commands)
            {
                if (!string.IsNullOrWhiteSpace(command))
                {
                    using (SqlCommand sqlCommand = new SqlCommand(command, connection))
                    {
                        sqlCommand.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}

