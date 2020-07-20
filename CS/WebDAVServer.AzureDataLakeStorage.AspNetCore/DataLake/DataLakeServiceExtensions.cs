using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Config;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.DataLake
{
    /// <summary>
    /// Extension which configures DI for data lake services.
    /// </summary>
    public static class DataLakeServiceExtensions
    {
        /// <summary>
        /// Adds Data lake service to container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/></param>
        /// <param name="configuration">Application configuration properties.</param>
        public static void AddDataLake(this IServiceCollection services, IConfigurationRoot configuration, IWebHostEnvironment env)
        {
            services.AddSingleton<IDataLakeStoreService, DataLakeStoreService>();
            services.Configure<DavContextConfig>(async config => await configuration.GetSection("Context").ReadConfigurationAsync(config, env));
        }
    }
}
