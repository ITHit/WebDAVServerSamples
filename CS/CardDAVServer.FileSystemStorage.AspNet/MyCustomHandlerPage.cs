
using System;
using System.Linq;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.CardDav;

namespace CardDAVServer.FileSystemStorage.AspNet
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
                DavContext context = new DavContext(HttpContext.Current);

                Discovery discovery = new Discovery(context);

                // Get all user address books Urls.
                // Get list of folders that contain user address books and enumerate address books in each folder.
                foreach (IItemCollectionAsync folder in await discovery.GetAddressbookHomeSetAsync())
                {
                    IEnumerable<IHierarchyItemAsync> children = await folder.GetChildrenAsync(new PropertyName[0]);
                    AllUserAddressbooks.AddRange(children.Where(x => x is IAddressbookFolderAsync));
                }
        }

        /// <summary>
        /// Gets all user address books.
        /// </summary>
        public List<IHierarchyItemAsync> AllUserAddressbooks = new List<IHierarchyItemAsync>();

        public static string ApplicationPath
        {
            get
            {
                    DavContext context = new DavContext(HttpContext.Current);
                    Uri url = HttpContext.Current.Request.Url;
                    string server = url.Scheme + "://" + url.Host + (url.IsDefaultPort ? "" : ":" + url.Port.ToString()) + "/" + context.Request.ApplicationPath.Trim('/');
                    return server.TrimEnd('/') + '/';
            }
        }
    }
}