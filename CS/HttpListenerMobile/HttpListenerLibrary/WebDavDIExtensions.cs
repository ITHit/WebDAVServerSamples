using ITHit.WebDAV.Server.Logger;
using Microsoft.Extensions.DependencyInjection;

namespace HttpListenerLibrary
{
    /// <summary>
    /// WebDav extension methods for dependency injection container.
    /// </summary>
    public static class WebDavDIExtensions
    {
        /// <summary>
        /// Adds configuration instance to DI container.
        /// </summary>
        /// <param name="serviceCollection">Services collection.</param>
        /// <param name="configuration">Configuration model instance.</param>
        public static void AddConfiguration(this IServiceCollection serviceCollection, JsonConfigurationModel configuration)
        {
            serviceCollection.AddSingleton(configuration);
        }

        /// <summary>
        /// Adds default logger implementation to DI container.
        /// </summary>
        /// <param name="serviceCollection">Services collection.</param>
        public static void AddLogger(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<DefaultLoggerImpl>();
        }
    }
}
