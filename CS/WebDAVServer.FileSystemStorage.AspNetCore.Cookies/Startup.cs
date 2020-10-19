using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using WebDAVServer.FileSystemStorage.AspNetCore.Cookies.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace WebDAVServer.FileSystemStorage.AspNetCore.Cookies
{
     public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews();
            services.AddRazorPages();
            //Adds a WebDAV services to the specified <see cref = "IServiceCollection"/>.
            services.AddWebDav(Configuration, HostingEnvironment);
            //Adds WebSocketsService which notifies client about changes in WebDAV items.
            services.AddSingleton<WebSocketsService>();
            //Adds a GSuite services to the specified <see cref="IServiceCollection"/>.
            services.AddGSuite(Configuration);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            //Adds built-in Net.Core web sockets middleware.
            app.UseWebSockets();
            //Adds middleware that submits notifications to clients when any item on a WebDAV server is modified using web sockets.
            app.UseWebSocketsMiddleware();
            //Conditional middleware use for server root in case of OPTIONS or PROPFIND request to server root.
            app.UseWhen(context =>
                             {
                                 return !context.Request.Path.StartsWithSegments("/DAV") && (context.Request.Method == "OPTIONS" || context.Request.Method == "PROPFIND");
                             }, webDavApp => webDavApp.UseMiddleware<DavEngineMiddleware>());

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
            //Adds a GSuite Engine middleware type to the application's request pipeline.
            app.UseGSuite();

            app.MapWhen(context =>
            {
                return context.Request.Path.StartsWithSegments("/DAV");
            }, webDavApp => webDavApp.UseMiddleware<DavEngineMiddleware>());
        }
        public IWebHostEnvironment HostingEnvironment
        {
            get;
        }
    }
}
