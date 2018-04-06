using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Routing;


namespace WebDAVServer.FileSystemStorage.AspNet.Cookies
{
    /// <summary>
    /// Forbids MVC routing of WebDAV requests that should be processed by WebDAV handler.
    /// </summary>
    /// <remarks>
    /// This module is needed for ASP.NET MVC application to forbid routing
    /// of WebDAV requests that should be processed by <see cref="DavHandler"/>.
    /// It reads DavLocations section and inserts ignore rules for urls that correspond
    /// to these locations.
    /// </remarks>
    public class UrlRoutingModule : IHttpModule
    {
        /// <summary>
        /// Represents route to be ignored. 
        /// </summary>
        /// <remarks>
        /// Required to insert ignore route at the beginning of routing table. We can not 
        /// use the <see cref="RouteCollection.IgnoreRoute"/> as it adds to the and of the 
        /// routing table.
        /// </remarks>
        private sealed class IgnoreRoute : Route
        {
            /// <summary>
            /// Creates ignore route.
            /// </summary>
            /// <param name="url">Route to be ignored.</param>
            public IgnoreRoute(string url)
                : base(url, new StopRoutingHandler())
            {
            }
            public override VirtualPathData GetVirtualPath(RequestContext requestContext, RouteValueDictionary routeValues)
            {
                return null;
            }
        }

        /// <summary>
        /// Indicates that the web application is started.
        /// </summary>
        private static bool appStarted = false;

        /// <summary>
        /// Application start lock.
        /// </summary>
        private static object appStartLock = new Object();

        void IHttpModule.Init(HttpApplication application)
        {
            // Here we update ASP.NET MVC routing table to avoid processing of WebDAV requests.
            // This should be done only one time during web application start event.

            lock (appStartLock)
            {
                if (appStarted)
                {
                    return;
                }
                appStarted = true;
            }

            NameValueCollection davLocationsSection = (NameValueCollection)System.Configuration.ConfigurationManager.GetSection("davLocations");

            int routeInsertPosition = 0;
            foreach (string path in davLocationsSection.AllKeys)
            {
                string verbs = davLocationsSection[path];
                string[] methods = verbs.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(verb => verb.Trim().ToUpper()).ToArray();

                string prefix = path.Trim(new[] { '/', '\\' });
                string ignoreRoutePath = (prefix == string.Empty ? string.Empty : prefix + "/") + "{*rest}";

                IgnoreRoute ignoreRoute = new IgnoreRoute(ignoreRoutePath);
                if (methods.Length > 0)
                {
                    ignoreRoute.Constraints = new RouteValueDictionary { { "httpMethod", new HttpMethodConstraint(methods) } };
                }
                RouteTable.Routes.Insert(routeInsertPosition++, ignoreRoute);
            }
        }

        public void Dispose()
        {
        }
    }
}
