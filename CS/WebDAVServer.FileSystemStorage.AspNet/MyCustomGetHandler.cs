using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;
using ITHit.Server.Extensibility;
using ITHit.Server;

namespace WebDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// This handler processes GET and HEAD requests to folders returning custom HTML page.
    /// </summary>
    internal class MyCustomGetHandler : IMethodHandler<IHierarchyItem>
    {
        /// <summary>
        /// Handler for GET and HEAD request registered with the engine before registering this one.
        /// We call this default handler to handle GET and HEAD for files, because this handler
        /// only handles GET and HEAD for folders.
        /// </summary>
        public IMethodHandler<IHierarchyItem> OriginalHandler { get; set; }

        /// <summary>
        /// Gets a value indicating whether output shall be buffered to calculate content length.
        /// Don't buffer output to calculate content length.
        /// </summary>
        public bool EnableOutputBuffering
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether engine shall log response data (even if debug logging is on).
        /// </summary>
        public bool EnableOutputDebugLogging
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the engine shall log request data.
        /// </summary>
        public bool EnableInputDebugLogging
        {
            get { return false; }
        }

        /// <summary>
        /// Path to the folder where HTML files are located.
        /// </summary>
        private readonly string htmlPath;

        /// <summary>
        /// Creates instance of this class.
        /// </summary>
        /// <param name="contentRootPathFolder">Path to the folder where HTML files are located.</param>
        public MyCustomGetHandler(string contentRootPathFolder)
        {
            this.htmlPath = contentRootPathFolder;
        }

        /// <summary>
        /// Handles GET and HEAD request.
        /// </summary>
        /// <param name="context">Instace of <see cref="ContextAsync{IHierarchyItem}"/>.</param>
        /// <param name="item">Instance of <see cref="IHierarchyItem"/> which was returned by
        /// <see cref="ContextAsync{IHierarchyItem}.GetHierarchyItemAsync"/> for this request.</param>
        public async Task ProcessRequestAsync(ContextAsync<IHierarchyItem> context, IHierarchyItem item)
        {
            string urlPath = context.Request.RawUrl.Substring(context.Request.ApplicationPath.TrimEnd('/').Length);

            if (item is IItemCollection)
            {
                // In case of GET requests to WebDAV folders we serve a web page to display 
                // any information about this server and how to use it.

                // Remember to call EnsureBeforeResponseWasCalledAsync here if your context implementation
                // makes some useful things in BeforeResponseAsync.
                await context.EnsureBeforeResponseWasCalledAsync();
                IHttpAsyncHandler page = (IHttpAsyncHandler)System.Web.Compilation.BuildManager.CreateInstanceFromVirtualPath(
                    "~/MyCustomHandlerPage.aspx", typeof(MyCustomHandlerPage));

                if(Type.GetType("Mono.Runtime") != null)
                {
                    page.ProcessRequest(HttpContext.Current);
                }
                else
                {
                    // Here we call BeginProcessRequest instead of ProcessRequest to start an async page execution and be able to call RegisterAsyncTask if required. 
                    // To call APM method (Begin/End) from TAP method (Task/async/await) the Task.FromAsync must be used.
                    await Task.Factory.FromAsync(page.BeginProcessRequest, page.EndProcessRequest, HttpContext.Current, null);
                }
            }
            else
            {
                await OriginalHandler.ProcessRequestAsync(context, item);
            }
        }

        /// <summary>
        /// This handler shall only be invoked for <see cref="IFolder"/> items or if original handler (which
        /// this handler substitutes) shall be called for the item.
        /// </summary>
        /// <param name="item">Instance of <see cref="IHierarchyItem"/> which was returned by
        /// <see cref="ContextAsync{IHierarchyItem}.GetHierarchyItemAsync"/> for this request.</param>
        /// <returns>Returns <c>true</c> if this handler can handler this item.</returns>
        public bool AppliesTo(IHierarchyItem item)
        {
            return item is IFolder || OriginalHandler.AppliesTo(item);
        }
    }
}