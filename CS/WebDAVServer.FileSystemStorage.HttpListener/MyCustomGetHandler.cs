using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using Microsoft.Win32;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;
using ITHit.Server.Extensibility;
using ITHit.Server;

namespace WebDAVServer.FileSystemStorage.HttpListener
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
                string htmlName = "MyCustomHandlerPage.html";
                string filePath = Path.Combine(htmlPath, htmlName);
                using (TextReader reader = File.OpenText(filePath))
                {
                    string html = await reader.ReadToEndAsync();
                    html = html.Replace("_webDavServerRoot_", context.Request.ApplicationPath.TrimEnd('/'));
                    html = html.Replace("_webDavServerVersion_",
                        typeof(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString());

                    await WriteFileContentAsync(context, html, htmlName);
                }
            }
            else if (urlPath.StartsWith("/AjaxFileBrowser/") || urlPath.StartsWith("/wwwroot/"))
            {
                // The "/AjaxFileBrowser/" are not a WebDAV folders. They can be used to store client script files, 
                // images, static HTML files or any other files that does not require access via WebDAV.
                // Any request to the files in this folder will just serve them to the client. 

                await context.EnsureBeforeResponseWasCalledAsync();
                string filePath = Path.Combine(htmlPath, urlPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

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

                Encoding encoding = context.Engine.ContentEncoding; // UTF-8 by default
                context.Response.ContentType = string.Format("{0}; charset={1}", MimeType.GetMimeType(Path.GetExtension(filePath)) ?? "application/octet-stream", encoding.WebName);

                // Return file content in case of GET request, in case of HEAD just return headers.
                if (context.Request.HttpMethod == "GET")
                {
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        context.Response.ContentLength = fileStream.Length;
                        await fileStream.CopyToAsync(context.Response.OutputStream);
                    }
                }
            }
            else
            {
                await OriginalHandler.ProcessRequestAsync(context, item);
            }
        }

        /// <summary>
        /// Writes HTML to the output stream in case of GET request using encoding specified in Engine. 
        /// Writes headers only in case of HEAD request.
        /// </summary>
        /// <param name="context">Instace of <see cref="ContextAsync{IHierarchyItem}"/>.</param>
        /// <param name="content">String representation of the content to write.</param>
        /// <param name="filePath">Relative file path, which holds the content.</param>
        private async Task WriteFileContentAsync(ContextAsync<IHierarchyItem> context, string content, string filePath)
        {
            Encoding encoding = context.Engine.ContentEncoding; // UTF-8 by default
            context.Response.ContentLength = encoding.GetByteCount(content);     
            context.Response.ContentType = string.Format("{0}; charset={1}", MimeType.GetMimeType(Path.GetExtension(filePath)) ?? "application/octet-stream", encoding.WebName);

            // Return file content in case of GET request, in case of HEAD just return headers.
            if (context.Request.HttpMethod == "GET")
            {               
                using (var writer = new StreamWriter(context.Response.OutputStream, encoding))
                {
                    await writer.WriteAsync(content);
                }
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