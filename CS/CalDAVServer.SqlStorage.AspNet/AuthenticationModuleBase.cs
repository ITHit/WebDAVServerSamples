using System;
using System.Security;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Security;

namespace CalDAVServer.SqlStorage.AspNet
{
    /// <summary>
    /// Base class for challenge/response authentication ASP.NET modules, like Digest, Basic.
    /// </summary>
    public abstract class AuthenticationModuleBase : IHttpModule
    {

        public void Init(HttpApplication application)
        {
            application.AuthenticateRequest += App_OnAuthenticateRequest;
            application.EndRequest += App_OnEndRequest;
        }

        public void Dispose()
        {
        }

        
        protected abstract IPrincipal AuthenticateRequest(HttpRequest request);
        
        protected abstract string GetChallenge();

        protected abstract bool IsAuthorizationPresent(HttpRequest request);
        
        private void App_OnAuthenticateRequest(object source, EventArgs eventArgs)
        {
            if (IsAuthorizationPresent(HttpContext.Current.Request))
            {
                IPrincipal principal = AuthenticateRequest(HttpContext.Current.Request);
                if (principal != null)
                { // authenticated succesfully
                    HttpContext.Current.User = principal;
                }
                else
                { // invalid credentials
                    unauthorized();
                }
            }
            else
            {
                // To support Miniredirector/Web Folders on XP and Server 2003 as well as 
                // Firefox CORS requests, OPTIONS must be processed without authorization.
                if (HttpContext.Current.Request.HttpMethod == "OPTIONS")
                    return;

                unauthorized();
            }
        }
        
        private void App_OnEndRequest(object source, EventArgs eventArgs)
        {

            HttpApplication app = (HttpApplication)source;
            if (app.Response.StatusCode == 401)
            { // show login dialog
                app.Response.AppendHeader("WWW-Authenticate", GetChallenge());
            }
        }

        private static void unauthorized()
        {
            HttpResponse response = HttpContext.Current.Response;
            response.StatusCode = 401;
            response.StatusDescription = "Unauthorized";
            response.Write("401 Unauthorized");
        }
    }

}
