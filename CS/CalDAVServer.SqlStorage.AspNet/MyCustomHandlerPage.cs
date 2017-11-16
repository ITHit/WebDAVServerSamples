
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CalDav;

namespace CalDAVServer.SqlStorage.AspNet
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
            using (DavContext context = new DavContext(HttpContext.Current))
            {

                Discovery discovery = new Discovery(context);

                // Get all user calendars Urls.
                // Get list of folders that contain user calendars and enumerate calendars in each folder.
                foreach (IItemCollectionAsync folder in await discovery.GetCalendarHomeSetAsync())
                {
                    IEnumerable<IHierarchyItemAsync> children = await folder.GetChildrenAsync(new PropertyName[0]);
                    AllUserCalendars.AddRange(children.Where(x => x is ICalendarFolderAsync));
                }
            }
        }

        /// <summary>
        /// Gets all user calendars.
        /// </summary>
        public List<IHierarchyItemAsync> AllUserCalendars = new List<IHierarchyItemAsync>();

        public static string ApplicationPath
        {
            get
            {
                using (DavContext context = new DavContext(HttpContext.Current))
                {
                    Uri url = HttpContext.Current.Request.Url;
                    string server = url.Scheme + "://" + url.Host + (url.IsDefaultPort ? "" : ":" + url.Port.ToString()) + "/" + context.Request.ApplicationPath.Trim('/');
                    return server.TrimEnd('/') + '/';
                }
            }
        }
    }
}