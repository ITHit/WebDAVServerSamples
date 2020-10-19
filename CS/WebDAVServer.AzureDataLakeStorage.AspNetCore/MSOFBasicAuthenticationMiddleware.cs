using System;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using RestSharp;
using RestSharp.Serialization.Json;
using WebDAVServer.AzureDataLakeStorage.AspNetCore.Config;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    public class MSOFBasicAuthenticationMiddleware
    {
        private RequestDelegate next;
        private IOptions<DavContextConfig> config;
        private IOptions<AzureAdConfig> adConfig;

        public MSOFBasicAuthenticationMiddleware(RequestDelegate next, IOptions<DavContextConfig> config, IOptions<AzureAdConfig> adConfig)
        {
            this.next = next;
            this.config = config;
            this.adConfig = adConfig;
        }

        public async Task Invoke(HttpContext context)
        {
            if (isOFBAAccepted(context))
            {
                if (context.User != null && context.User.Identity.IsAuthenticated)
                {
                    await next(context);
                }
                else
                {
                    var basicAuth = GetBasicAuth(context);
                    if (basicAuth != null)
                    {
                        var client = new RestClient(adConfig.Value.Instance + adConfig.Value.TenantId + "/oauth2/v2.0/token");
                        var request = new RestRequest(Method.POST);
                        request.AddHeader("content-type", "application/x-www-form-urlencoded");
                        request.AddParameter("client_id", adConfig.Value.ClientId);
                        request.AddParameter("scope", "openid profile offline_access https://" + config.Value.AzureStorageAccountName + ".blob.core.windows.net/.default");
                        request.AddParameter("username", basicAuth.Username);
                        request.AddParameter("password", basicAuth.Password);
                        request.AddParameter("grant_type", "password");
                        IRestResponse response = client.Execute(request);
                        JsonDeserializer deserial = new JsonDeserializer();
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Datum returnData = deserial.Deserialize<Datum>(response);
                            if (returnData.id_token != null)
                            {
                                var token = returnData.access_token;
                                var claimsIdentity = new ClaimsIdentity("Bearer");
                                claimsIdentity.AddClaim(new Claim("access_token", token));
                                context.User = new ClaimsPrincipal(claimsIdentity);
                                await next(context);
                            }
                        }
                        else
                        {
                            Unauthorized(context);
                        }

                    }
                    else
                    {
                        Unauthorized(context);
                    }
                }
            }
            else
            {
                await next(context);
            }
        }

        protected void Unauthorized(HttpContext context)
        {
            context.Response.OnStarting(SetAuthenticationHeader, context);
            context.Response.StatusCode = 401;
            context.Response.WriteAsync("401 Unauthorized");
        }

        private Task SetAuthenticationHeader(object context)
        {
            HttpContext httpContext = (HttpContext) context;
            httpContext.Response.Headers.Append(HeaderNames.WWWAuthenticate, "Basic realm=" + $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.PathBase}");
            return Task.FromResult(0);
        }

        private bool isOFBAAccepted(HttpContext context)
        {
            // In case application provided X-FORMS_BASED_AUTH_ACCEPTED header
            string ofbaAccepted = context.Request.Headers["X-FORMS_BASED_AUTH_ACCEPTED"];
            if ((ofbaAccepted != null) && ofbaAccepted.Equals("T", StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }

            // Microsoft Office does not submit X-FORMS_BASED_AUTH_ACCEPTED header, but it still supports MS-OFBA,
            // Microsoft Office includes "Microsoft Office" string into User-Agent header
            string userAgent = context.Request.Headers["User-Agent"];
            if ((userAgent != null) && userAgent.Contains("Microsoft Office"))
            {
                return true;
            }

            return false;
        }

        private static AuthData GetBasicAuth(HttpContext context)
        {
            if (context.Request.Headers["Authorization"].Count > 0)
            {
                var header = context.Request.Headers["Authorization"][0];
                if (header.StartsWith("Basic"))
                {
                    header = header.Substring("Basic".Length + 1);
                    var usPas = Encoding.GetEncoding("UTF-8")
                        .GetString(Convert.FromBase64String(header));
                    var usPassArray = usPas.Split(":");
                    var username = usPassArray[0];
                    var password = usPassArray[1];
                    return new AuthData()
                    {
                        Username = username, 
                        Password = password
                    };
                }
            }

            return null;
        }
    }

    internal class AuthData
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    internal class Datum
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
        public string refresh_token { get; set; }
        public string token_type { get; set; }
        public string id_token { get; set; }
        public int id { get; set; }
    }

    public static class BasicAuthMiddlewareExtensions
    {
        /// <summary>
        /// Add Basic Authentication middleware.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseMSOFBasicAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<MSOFBasicAuthenticationMiddleware>();
        }
    }
}