
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;

namespace WebDAVServer.FileSystemStorage.AspNet
{
    public class MyCustomHandlerPage : Page
    {
        protected MyCustomHandlerPage()
        {
            if (Type.GetType("Mono.Runtime") == null)
            {
                this.Load += Page_LoadAsync;
            }
        }

        void Page_LoadAsync(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(GetPageDataAsync));
        }
        public async Task GetPageDataAsync()
        {
        }
    }
}