using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;

namespace WebDAVServer.NetCore.SqlStorage
{
    /// <summary>
    /// This handler processes GET and HEAD requests to folders returning custom HTML page.
    /// </summary>
    internal class MyCustomGetHandler : IMethodHandlerAsync
    {
        private readonly string contentRootPath;

        public MyCustomGetHandler(string contentRootPath)
        {
            this.contentRootPath = contentRootPath;
        }

        /// <summary>
        /// Handler for GET and HEAD request registered with the engine before registering this one.
        /// We call this default handler to handle GET and HEAD for files, because this handler
        /// only handles GET and HEAD for folders.
        /// </summary>
        public IMethodHandlerAsync OriginalHandler { get; set; }

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
        /// Handles GET and HEAD request.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="item">Instance of <see cref="IHierarchyItemAsync"/> which was returned by
        /// <see cref="DavContextBaseAsync.GetHierarchyItemAsync"/> for this request.</param>
        public async Task ProcessRequestAsync(DavContextBaseAsync context, IHierarchyItemAsync item)
        {
            if (item is IItemCollectionAsync)
            {
                // In case of GET requests to WebDAV folders we serve a web page to display 
                // any information about this server and how to use it.

                // Remember to call EnsureBeforeResponseWasCalledAsync here if your context implementation
                // makes some useful things in BeforeResponseAsync.
                await context.EnsureBeforeResponseWasCalledAsync();


                using (TextReader reader = File.OpenText(Path.Combine(contentRootPath, "MyCustomHandlerPage.html")))
                {
                    string html = await reader.ReadToEndAsync();
                    html = html.Replace("_webDavServerRoot_", context.Request.ApplicationPath.TrimEnd('/'));
                    html = html.Replace("_webDavServerVersion_", typeof(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString());

                    await WriteHtmlAsync(context, html);
                }
            }
            else if (context.Request.RawUrl.StartsWith("/AjaxFileBrowser/"))
            {
                // The "/AjaxFileBrowser/" is not a WebDAV folder. It can be used to store client script files, 
                // images, static HTML files or any other files that does not require access via WebDAV.
                // Any request to the files in this folder will just serve them to client. 

                await context.EnsureBeforeResponseWasCalledAsync();
                string filePath = Path.Combine(contentRootPath, context.Request.RawUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                // Remove query string.
                int queryIndex = filePath.LastIndexOf('?');
                if (queryIndex > -1)
                {
                    filePath = filePath.Remove(queryIndex);
                }

                if (!File.Exists(filePath))
                {
                    throw new DavException("File not found: " + filePath, DavStatus.NOT_FOUND);
                }

                using (TextReader reader = File.OpenText(filePath))
                {
                    string html = await reader.ReadToEndAsync();
                    await WriteHtmlAsync(context, html);
                }
            }
            else
            {
                await OriginalHandler.ProcessRequestAsync(context, item);
            }
        }

        /// <summary>
        /// Writes HTML to the output stream in case of GET request using encoding specified in Engine. 
        /// Writes headers only in caes of HEAD request.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="html">HTML to write.</param>
        private async Task WriteHtmlAsync(DavContextBaseAsync context, string html)
        {
            Encoding encoding = context.Engine.ContentEncoding; // UTF-8 by default
            context.Response.ContentLength = encoding.GetByteCount(html);
            context.Response.ContentType = string.Format("text/html; charset={0}", encoding.WebName);

            // Return file content in case of GET request, in case of HEAD just return headers.
            if (context.Request.HttpMethod == "GET")
            {
                using (var writer = new StreamWriter(context.Response.OutputStream, encoding))
                {
                    await writer.WriteAsync(html);
                }
            }
        }

        /// <summary>
        /// This handler shall only be invoked for <see cref="IFolderAsync"/> items or if original handler (which
        /// this handler substitutes) shall be called for the item.
        /// </summary>
        /// <param name="item">Instance of <see cref="IHierarchyItemAsync"/> which was returned by
        /// <see cref="DavContextBaseAsync.GetHierarchyItemAsync"/> for this request.</param>
        /// <returns>Returns <c>true</c> if this handler can handler this item.</returns>
        public bool AppliesTo(IHierarchyItemAsync item)
        {
            return item is IFolderAsync || OriginalHandler.AppliesTo(item);
        }
    }
}