using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using System.Threading.Tasks;

using ITHit.Server;
using ITHit.WebDAV.Server;

namespace WebDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// This handler processes WebDAV requests.
    /// </summary>
    public class DavHandler : HttpTaskAsyncHandler
    {
        /// <summary>
        /// This license file is used to activate:
        ///  - IT Hit WebDAV Server Engine for .NET
        ///  - IT Hit iCalendar and vCard Library if used in a project
        /// </summary>
        private readonly string license = File.ReadAllText(HttpContext.Current.Request.PhysicalApplicationPath + "License.lic");

        /// <summary>
        /// If debug logging is enabled reponses are output as formatted XML,
        /// all requests and response headers and most bodies are logged.
        /// If debug logging is disabled only errors are logged.
        /// </summary>
        private static readonly bool debugLoggingEnabled =
            "true".Equals(
                ConfigurationManager.AppSettings["DebugLoggingEnabled"],
                StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Gets a value indicating whether another request can use the
        /// <see cref="T:System.Web.IHttpHandler"/> instance.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Web.IHttpHandler"/> instance is reusable; otherwise, false.
        /// </returns>
        public override bool IsReusable
        {
            get { return true; }
        }
      

        /// <summary>
        /// Enables processing of HTTP Web requests.
        /// </summary>
        /// <param name="context">An <see cref="T:System.Web.HttpContext"/> object that provides references to the
        /// intrinsic server objects (for example, Request, Response, Session, and Server) used to service
        /// HTTP requests. 
        /// </param>
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            DavEngineAsync webDavEngine = getOrInitializeWebDavEngine(context);

            context.Response.BufferOutput = false;

            using (var sqlDavContext = new DavContext(context))
            {
                await webDavEngine.RunAsync(sqlDavContext);
            }
        }

        /// <summary>
        /// Initializes engine.
        /// </summary>
        /// <param name="context">Instance of <see cref="HttpContext"/>.</param>
        /// <returns>Initialized <see cref="DavEngine"/>.</returns>
        private DavEngineAsync initializeWebDavEngine(HttpContext context)
        {

            ILogger logger = WebDAVServer.SqlStorage.AspNet.Logger.Instance;
            DavEngineAsync webDavEngine = new DavEngineAsync
            {
                Logger = logger

                // Use idented responses if debug logging is enabled.
                , OutputXmlFormatting = true
            };

            webDavEngine.License = license;
            string contentRootPath = HttpContext.Current.Request.MapPath("/");

            // Set custom handler to process GET and HEAD requests to folders and display 
            // info about how to connect to server. We are using the same custom handler 
            // class (but different instances) here to process both GET and HEAD because 
            // these requests are very similar. Some WebDAV clients may fail to connect if HEAD 
            // request is not processed.
            MyCustomGetHandler handlerGet  = new MyCustomGetHandler(contentRootPath);
            MyCustomGetHandler handlerHead = new MyCustomGetHandler(contentRootPath);
            handlerGet.OriginalHandler  = webDavEngine.RegisterMethodHandler("GET",  handlerGet);
            handlerHead.OriginalHandler = webDavEngine.RegisterMethodHandler("HEAD", handlerHead);

            return webDavEngine;
        }

        /// <summary>
        /// Initializes or gets engine singleton.
        /// </summary>
        /// <param name="context">Instance of <see cref="HttpContext"/>.</param>
        /// <returns>Instance of <see cref="DavEngineAsync"/>.</returns>
        private DavEngineAsync getOrInitializeWebDavEngine(HttpContext context)
        {
            //we don't use any double check lock pattern here because nothing wrong
            //is going to happen if we created occasionally several engines.
            const string ENGINE_KEY = "$DavEngine$";
            if (context.Application[ENGINE_KEY] == null)
            {
                context.Application[ENGINE_KEY] = initializeWebDavEngine(context);
            }

            return (DavEngineAsync)context.Application[ENGINE_KEY];
        }
    }
}
