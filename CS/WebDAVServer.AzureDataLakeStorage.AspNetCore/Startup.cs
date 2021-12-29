using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.AzureAD.UI;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using WebDAVServer.AzureDataLakeStorage.AspNetCore.MSOFBAuthentication;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("appsettings.webdav.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true);

            HostingEnvironment = env;
            builder.AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }
        public IWebHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure Azure AD authentication.
            services.AddAuthentication(AzureADDefaults.AuthenticationScheme)
                .AddAzureAD(options => Configuration.Bind("AzureAd", options));
            services.Configure<OpenIdConnectOptions>(AzureADDefaults.OpenIdScheme, options =>
            {
                var config = Configuration.GetSection("AzureAd").Get<OpenIdConnectOptions>();
                options.ResponseType = config.ResponseType;
                options.Resource = config.Resource;
                options.SaveTokens = config.SaveTokens;

                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        var accessToken = context.ProtocolMessage.AccessToken;
                        if (accessToken != null)
                        {
                            if (context.Principal.Identity is ClaimsIdentity identity)
                            {
                                identity.AddClaim(new Claim("access_token", accessToken));
                                identity.AddClaim(new Claim("access_token_expires_in", context.ProtocolMessage.ExpiresIn));
                            }
                        }
                        return Task.CompletedTask;
                    },
                    OnTicketReceived = context =>
                    {
                        if (context.Principal.Identity is ClaimsIdentity identity)
                        {
                            context.Properties.IsPersistent = true;
                            // set an expiration date for a cookie
                            context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.AddSeconds(int.Parse(identity.Claims.Where(p => p.Type == "access_token_expires_in").First().Value));
                        }

                        return Task.FromResult(0);
                    }
                };
            });

            services.AddWebDav(Configuration, HostingEnvironment);

            //Adds a MS-OFBA configuration to the specified <see cref = "IServiceCollection"/>.
            services.AddMSOFBA(Configuration);

            //Enables web sockets. Web sockets are used to update the documents list in case of any changes on the server.
            services.AddSingleton<WebSocketsService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseStaticFiles(new StaticFileOptions { ServeUnknownFileTypes = true });

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseAuthentication();
            app.UseAuthorization();

            //Add Microsoft Office Forms Based Authentication middleware.
            app.UseMSOFBA();

            //Enables web sockets. Web sockets are used to update the documents list in case of any changes on the server.
            app.UseWebSockets();
            app.UseWebSocketsMiddleware();
            app.UseWebDav(HostingEnvironment);
        }
    }
}
