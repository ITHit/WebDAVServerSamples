using System;
using System.Drawing;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CardDAVServer.SqlStorage.AspNetCore.MSOFBAuthentication
{
    public static class MSOFBAMiddlewareExtensions
    {
        private static IConfiguration Configuration;

        /// <summary>
        /// Adds a MSOFBA services configuration to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> instance.</param>
        /// <param name="configuration">The <see cref="IConfigurationRoot"/> instance.</param>
        public static void AddMSOFBA(this IServiceCollection services, IConfiguration configuration)
        {
            Configuration = configuration;

            services.Configure<MSOFBAuthenticationConfig>(config => Configuration.GetSection("MSOFBAuthentication").Bind(config));
        }

        /// <summary>
        /// Adds a MSOFBA middleware type to the application's request pipeline.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance. </returns>
        public static IApplicationBuilder UseMSOFBA(this IApplicationBuilder builder)
        {
            var options = builder.ApplicationServices.GetService<IOptions<MSOFBAuthenticationConfig>>();
            //setup default values 
            SetupDefaultValues(options);
            builder.UseMiddleware<MSOFBAuthenticationMiddleware>();

            return builder;
        }

        private static void SetupDefaultValues(IOptions<MSOFBAuthenticationConfig> config)
        {
            if (!config.Value.LoginPath.HasValue)
            {
                config.Value.LoginPath = "/Identity/Account/Login";
            }

            if (!config.Value.LoginSuccessPath.HasValue)
            {
                config.Value.LoginSuccessPath = new PathString("/");
            }

            if (string.IsNullOrEmpty(config.Value.ReturnUrlParameter))
            {
                config.Value.ReturnUrlParameter = CookieAuthenticationDefaults.ReturnUrlParameter;
            }

            if (config.Value.DialogSize.IsEmpty)
            {
                config.Value.DialogSize = new Size(800, 600);
            }
        }
    }
}
