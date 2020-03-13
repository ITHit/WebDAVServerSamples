using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore;

namespace WebDAVServer.FileSystemStorage.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
                        WebHost.CreateDefaultBuilder(args)
                            .UseStartup<Startup>();
    }
}
