using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(WebDAVServer.FileSystemStorage.AspNet.Cookies.Startup))]
namespace WebDAVServer.FileSystemStorage.AspNet.Cookies
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
