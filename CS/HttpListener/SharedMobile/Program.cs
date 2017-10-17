using HttpListenerLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace SharedMobile
{
    /// <summary>
    /// Starting point of HttpListener.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Starts HttpListener for listening to requests.
        /// </summary>
        /// <param name="logMethod">Represents method, which performs logging to application view.</param>
        /// <param name="configurationHelper">Represents json configuration inmemory collection with function to get file content on specific platform.</param>
        public static void Main(ILogMethod logMethod, IConfigurationHelper configurationHelper)
        {
            var host = new WebHostBuilder()
                .ConfigureServices(s =>
                {
                    s.AddSingleton(logMethod);
                    s.AddSingleton(configurationHelper);
                })
                .UseHttpListener()
                .UseContentRoot(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
