using System.Web;
using System.Web.Mvc;

namespace WebDAVServer.FileSystemStorage.AspNet.Cookies
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}
