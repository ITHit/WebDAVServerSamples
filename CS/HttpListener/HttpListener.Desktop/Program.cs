using HttpListenerLibrary;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace HttpListener.Desktop
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .Build();

            var host = new WebHostBuilder()
                .ConfigureServices(s => s.AddSingleton<ILogMethod, DesktopLogMethod>())
                .UseHttpListener()
                .UseContentRoot(Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory()).FullName, "HttpListenerShared"))
                .UseConfiguration(configuration)
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
