using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;
using ITHit.Server.Extensibility;
using ITHit.Server;

namespace WebDAVServer.FileSystemStorage.AspNetCore.Cookies
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
                using (TextReader reader = File.OpenText(Path.Combine(htmlPath, htmlName)))
                {
                    string html = await reader.ReadToEndAsync();
                    html = html.Replace("_webDavServerRoot_", "");
                    html = html.Replace("_webDavServerVersion_",
                        typeof(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString());

                    // Set list of cookie names for ajax lib.
                    if (context.Request.Headers.ContainsKey("Cookie"))
                    {
                        html = html.Replace("_webDavAuthCookieNames_", string.Join(",", context.Request.Headers["Cookie"]
                                    .TrimEnd(';').Split(';').Select(p => p.Split(new[] { '=' }, 2)[0].Trim())
                                    .Where(p => p.StartsWith(".AspNetCore.Identity.Application") || p.StartsWith(".AspNetCore.Cookies") || p.StartsWith(".AspNetCore.AzureADCookie"))));
                    }

                    await WriteFileContentAsync(context, html, htmlName);
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
                await using (var writer = new StreamWriter(context.Response.OutputStream, encoding))
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