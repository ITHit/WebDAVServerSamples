using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace WebDAVServer.SqlStorage.AspNetCore
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.webdav.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);
            /*
            if (env.IsDevelopment())
            {
                // For more details on using the user secret store see http://go.microsoft.com/fwlink/?LinkID=532709
                builder.AddUserSecrets();
            }
            */
            HostingEnvironment = env;
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        public IHostingEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddWebDav(Configuration, HostingEnvironment);
            services.AddSingleton<WebSocketsService>();
            services.Configure<DavUsersOptions>(options => Configuration.GetSection("DavUsers").Bind(options));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            // Basic auth requires SSL connection. To enable non - SSL connection for testing purposes read the following articles:
            // - In case of Windows & MS Office: http://support.microsoft.com/kb/2123563
            // - In case of Mac OS X & MS Office: https://support.microsoft.com/en-us/kb/2498069
            //app.UseBasicAuth();
            //app.UseDigestAuth();
            app.UseWebSockets();
            app.UseWebSocketsMiddleware();

            app.UseWebDav(HostingEnvironment);
        }
    }
}
