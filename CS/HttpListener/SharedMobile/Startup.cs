using HttpListenerLibrary;
using HttpListenerLibrary.Options;
using ITHit.WebDAV.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharedMobile
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IConfigurationHelper configurationHelper)
        {
            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(configurationHelper.JsonValuesCollection);
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
            services.AddSingleton<DavEngineAsync, DavEngineCoreMobile>();
            services.AddSingleton<EventsService>();
            services.Configure<DavUserOptions>(options => Configuration.GetSection("DavUsers").Bind(options));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            
        }
    }
}
