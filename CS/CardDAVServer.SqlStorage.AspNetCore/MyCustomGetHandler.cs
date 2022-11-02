using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.StaticFiles;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;
using ITHit.WebDAV.Server.CardDav;
using ITHit.Server.Extensibility;
using ITHit.Server;

namespace CardDAVServer.SqlStorage.AspNetCore
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

                // Request to iOS/OS X CalDAV/CardDAV profile.
                if (context.Request.RawUrl.EndsWith("?connect"))
                {
                    await WriteProfileAsync(context, item, htmlPath);
                    return;
                }

                string htmlName = "MyCustomHandlerPage.html";
                using (TextReader reader = File.OpenText(Path.Combine(htmlPath, htmlName)))
                {
                    string html = await reader.ReadToEndAsync();
                    html = html.Replace("_webDavServerUrl_", context.Request.UrlPrefix + context.Request.ApplicationPath);
                    html = html.Replace("_BOOKS_", await AllUserAddressbooksUrlAsync(context));
                    html = html.Replace("_webDavServerRoot_", context.Request.ApplicationPath.TrimEnd('/'));
                    html = html.Replace("_webDavServerVersion_",
                        typeof(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString());

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

        /// <summary>
        /// Gets all user address books URLs.
        /// </summary>
        private async Task<string> AllUserAddressbooksUrlAsync(ContextAsync<IHierarchyItem> context)
        {
            Discovery discovery = new Discovery(context as DavContext);
            List<IHierarchyItem> items = new List<IHierarchyItem>();

            // get list of folders that contain user address books and enumerate address books in each folder
            foreach (IItemCollection folder in await discovery.GetAddressbookHomeSetAsync())
            {
                IEnumerable<IHierarchyItem> children = (await folder.GetChildrenAsync(new PropertyName[0], null, null, null)).Page;
                items.AddRange(children.Where(x => x is IAddressbookFolder));
            }
            IEnumerable<string> nameAndUrls = items.Select(x => string.Format("<tr><td><span class=\"glyphicon glyphicon - book\"></span></td><td>{0}</td><td>{1}</td><td><a href=\"{1}?connect\" class=\"btn btn-default\">Connect</a></td></tr>", x.Name, AddAppPath(context, x.Path)));
            return String.Join(string.Empty, nameAndUrls.ToArray());
        }

        private static string AddAppPath(ContextAsync<IHierarchyItem> context, string path)
        {
            string applicationPath = context.Request.UrlPrefix + context.Request.ApplicationPath;
            return string.Format("{0}/{1}", applicationPath.TrimEnd(new[] { '/' }), path.TrimStart(new[] { '/' }));
        }

        /// <summary>
        /// Writes iOS / OS X CalDAV/CardDAV profile.
        /// </summary>
        /// <param name="context">Instace of <see cref="ContextAsync{IHierarchyItem}"/>.</param>
        /// <param name="item">ICalendarFolder or IAddressbookFolder item.</param>
        /// <returns></returns>
        private async Task WriteProfileAsync(ContextAsync<IHierarchyItem> context, IHierarchyItemBase item, string htmlPath)
        {
            string mobileconfigFileName = null;
            string decription = null;
            if (item is IAddressbookFolder)
            {
                mobileconfigFileName = "CardDAV.AppleProfileTemplete.mobileconfig";
                decription = (item as IAddressbookFolder).AddressbookDescription;
            }

            decription = !string.IsNullOrEmpty(decription) ? decription : item.Name;

            string templateContent = null;
            using (TextReader reader = new StreamReader(Path.Combine(htmlPath, mobileconfigFileName)))
            {
                templateContent = await reader.ReadToEndAsync();
            }

            Uri url = new Uri(context.Request.UrlPrefix);

            string payloadUUID = item.Path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries).Last(); // PayloadUUID

            string profile = string.Format(templateContent
                , url.Host // host name
                , item.Path // CalDAV / CardDAV Principal URL. Here we can return (await (item as ICurrentUserPrincipal).GetCurrentUserPrincipalAsync()).Path if needed.
                , (context as DavContext).Identity.Name // user name
                , url.Port // port                
                , (url.Scheme == "https").ToString().ToLower() // SSL
                , decription // CardDAV / CardDAV Account Description
                , Assembly.GetAssembly(this.GetType()).GetName().Version.ToString()
                , Assembly.GetAssembly(typeof(DavEngineAsync)).GetName().Version.ToString()
                , payloadUUID
                );

            byte[] profileBytes = SignProfile(context, profile);

            context.Response.ContentType = "application/x-apple-aspen-config";
            context.Response.AddHeader("Content-Disposition", "attachment; filename=profile.mobileconfig");
            context.Response.ContentLength = profileBytes.Length;
            await context.Response.OutputStream.WriteAsync(profileBytes, 0, profileBytes.Length);
        }

        /// <summary>
        /// Signs iOS / OS X payload profile with SSL certificate.
        /// </summary>
        /// <param name="context">Instace of <see cref="ContextAsync{IHierarchyItem}"/>.</param>
        /// <param name="profile">Profile to sign.</param>
        /// <returns>Signed profile.</returns>
        private byte[] SignProfile(ContextAsync<IHierarchyItem> context, string profile)
        {
            // Here you will sign your profile with SSL certificate to avoid "Unsigned" warning on iOS and OS X.
            // For demo purposes we just return the profile content unmodified.
            return context.Engine.ContentEncoding.GetBytes(profile);
        }
    }
}