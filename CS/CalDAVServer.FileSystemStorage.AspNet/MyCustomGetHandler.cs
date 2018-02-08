using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;
using ITHit.WebDAV.Server.CalDav;

namespace CalDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// This handler processes GET and HEAD requests to folders returning custom HTML page.
    /// </summary>
    internal class MyCustomGetHandler : IMethodHandlerAsync
    {
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
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="item">Instance of <see cref="IHierarchyItemAsync"/> which was returned by
        /// <see cref="DavContextBaseAsync.GetHierarchyItemAsync"/> for this request.</param>
        public async Task ProcessRequestAsync(DavContextBaseAsync context, IHierarchyItemAsync item)
        {
            string urlPath = context.Request.RawUrl.Substring(context.Request.ApplicationPath.TrimEnd('/').Length);

            if (item is IItemCollectionAsync)
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

        /// <summary>
        /// Writes iOS / OS X CalDAV/CardDAV profile.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="item">ICalendarFolderAsync or IAddressbookFolderAsync item.</param>
        /// <returns></returns>
        private async Task WriteProfileAsync(DavContextBaseAsync context, IHierarchyItemAsync item, string htmlPath)
        {
            string mobileconfigFileName = null;
            string decription = null;
            if (item is ICalendarFolderAsync)
            {
                mobileconfigFileName = "CalDAV.AppleProfileTemplete.mobileconfig";
                decription = (item as ICalendarFolderAsync).CalendarDescription;
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
                , item.Path // CalDAV / CardDAV Principal URL. Here we can return (await (item as ICurrentUserPrincipalAsync).GetCurrentUserPrincipalAsync()).Path if needed.
                , (context as DavContext).UserName // user name
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
            using (BinaryWriter writer = new BinaryWriter(context.Response.OutputStream))
            {
                writer.Write(profileBytes);
            }
        }

        /// <summary>
        /// Signs iOS / OS X payload profile with SSL certificate.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="profile">Profile to sign.</param>
        /// <returns>Signed profile.</returns>
        private byte[] SignProfile(DavContextBaseAsync context, string profile)
        {
            // Here you will sign your profile with SSL certificate to avoid "Unsigned" warning on iOS and OS X.
            // For demo purposes we just return the profile content unmodified.
            return context.Engine.ContentEncoding.GetBytes(profile);
        }
    }
}