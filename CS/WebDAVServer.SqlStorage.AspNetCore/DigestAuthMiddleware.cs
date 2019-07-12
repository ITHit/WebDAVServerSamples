using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebDAVServer.SqlStorage.AspNetCore
{
    /// <summary>
    /// Middleware which implements 'Digest' authentication protocol.
    /// </summary>
    public class DigestAuthMiddleware : AuthenticationBase
    {
        private bool isNonceStale = false;

        /// <summary>
        /// Next middleware instance.
        /// </summary>
        private readonly RequestDelegate next;

        /// <summary>
        /// Represents name of digest authentication protocol.
        /// </summary>
        protected override string AuthenicationProvider { get { return "Digest"; } }

        /// <summary>
        /// Initializes new instance of this class.
        /// </summary>
        /// <param name="next">Next middleware instance.</param>
        /// <param name="options">Users Options.</param>
        public DigestAuthMiddleware(RequestDelegate next, IOptions<DavUsersOptions> options) : base(options)
        {
            this.next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            // If Authorize header is present - perform request authenticating.
            if (IsAuthorizationPresent(context.Request))
            {
                ClaimsPrincipal userPrincipal = AuthenticateRequest(context.Request);
                if (userPrincipal.Identity != null)
                {
                    // Authenticated succesfully.
                    context.User = userPrincipal;
                    await next(context);
                }
                else
                {
                    // Invalid credentials.
                    Unauthorized(context);
                    return;
                }
            }
            else
            {
                // To support Miniredirector/Web Folders on XP and Server 2003 as well as 
                // Firefox CORS requests, OPTIONS must be processed without authorization.
                // MS Office for Mac requires OPTIONS request to be authenticated.
                if (context.Request.Method == "OPTIONS" &&
                    !(context.Request.Headers["User-Agent"].ToString().StartsWith("Microsoft Office")))
                {
                    await next(context);
                }
                else
                {
                    Unauthorized(context);
                    return;
                }
            }
        }

        /// <summary>
        /// Performs request with digest authentication.
        /// </summary>
        /// <param name="request">Instance of <see cref="HttpRequest"/>.</param>
        /// <returns>Instance of <see cref="ClaimsPrincipal"/>, or <c>null</c> if user was not authenticated.</returns>
        protected override ClaimsPrincipal AuthenticateRequest(HttpRequest request)
        {
            // Getting authorize header string.
            string headerString = request.Headers[HeaderNames.Authorization].ToString();
            string authFragment = headerString.Substring(AuthenicationProvider.Length + 1).Trim();

            //Filling header segments in dictionary.
            Dictionary<string, string> reqInfo = new Dictionary<string, string>();
            MatchCollection matches = Regex.Matches(authFragment, "([a-zA-Z0-9]+)=(([^\",]+)|(\\\"([^\"]*)\\\")),?");
            // Group 1 - Param name
            // Group 2 - Whole value string
            // Group 3 - Value if not in "\"" e.g. alg=MD5
            // Group 5 - Value if in "\"" e.g. name="name"
            foreach (Match match in matches)
            {
                string value = match.Groups[5].Value;
                if(value.Length == 0)
                {
                    value = match.Groups[3].Value;
                }
                reqInfo.Add(match.Groups[1].Value, value);
            }

            string clientUsername = reqInfo.ContainsKey("username") ? reqInfo["username"] : string.Empty;

            // Workaround for Windows Vista Digest Authorization. User name may be submitted in the following format:
            // Machine\\User.
            clientUsername = clientUsername.Replace("\\\\", "\\");
            reqInfo["username"] = clientUsername;
            string username = clientUsername;
            int ind = username.LastIndexOf('\\');
            if (ind > 0)
            {
                username = username.Remove(0, ind + 1);
            }

            // Finding user password.
            string par = getUserPassword(username);
            if (par == null)
            {
                return new ClaimsPrincipal(); 
            }

            // Generate hash.
            string unhashedDigest = generateUnhashedDigest(par, reqInfo, request.Method);
            string hashedDigest = createMD5HashBinHex(unhashedDigest);

            isNonceStale = !isNonceValid(reqInfo["nonce"]);

            if ((reqInfo["response"] != hashedDigest) || isNonceStale)
            {
                return new ClaimsPrincipal(); 
            }

            // Authenticate user.
            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.NameIdentifier, username));
            claims.Add(new Claim(ClaimTypes.Name, username));
            ClaimsIdentity identity = new ClaimsIdentity(claims, "Digest");
            return new ClaimsPrincipal(identity);
        }

        /// <summary>
        /// Sets authentication header to request digest authentication and show login dialog.
        /// </summary>
        /// <param name="context">Instance of current context.</param>
        /// <returns>Successfull task result.</returns>
        protected override Task SetAuthenticationHeader(object context)
        {
            HttpContext httpContext = (HttpContext)context;
            string nonce = createNewNonce();
            StringBuilder stringBuilder = new StringBuilder(AuthenicationProvider);
            stringBuilder.Append(" realm=\"");
            stringBuilder.Append(realm);
            stringBuilder.Append("\", nonce=\"");
            stringBuilder.Append(nonce);
            stringBuilder.Append("\", opaque=\"0000000000000000\", stale=");
            stringBuilder.Append(isNonceStale ? "true" : "false");
            stringBuilder.Append(", algorithm=MD5, qop=\"auth\"");
            httpContext.Response.Headers.Append(HeaderNames.WWWAuthenticate, stringBuilder.ToString());
            return Task.FromResult(0);
        }

        /// <summary>
        /// Creates new server nonce.
        /// </summary>
        /// <returns>New server nonce.</returns>
        private string createNewNonce()
        {
            DateTime nonceTime = DateTime.Now + TimeSpan.FromMinutes(1);
            string expireStr = nonceTime.ToString("G");

            byte[] expireBytes = Encoding.ASCII.GetBytes(expireStr);
            string nonce = Convert.ToBase64String(expireBytes);

            nonce = nonce.TrimEnd(new char[] { '=' });
            return nonce;
        }

        /// <summary>
        /// Generate digest response.
        /// </summary>
        /// <param name="password">User password.</param>
        /// <param name="reqInfo">Dictionary with authentication header segments.</param>
        /// <param name="httpMethod">Request http method.</param>
        /// <returns>Unhashed digest.</returns>
        private string generateUnhashedDigest(
            string password,
            Dictionary<string, string> reqInfo,
            string httpMethod)
        {
            string a1 = string.Format("{0}:{1}:{2}", reqInfo["username"], realm, password);
            string ha1 = createMD5HashBinHex(a1);
            string a2 = string.Format("{0}:{1}", httpMethod, reqInfo["uri"]);
            string ha2 = createMD5HashBinHex(a2);

            string unhashedDigest;
            if (reqInfo["qop"] != null)
            {
                unhashedDigest = string.Format(
                    "{0}:{1}:{2}:{3}:{4}:{5}",
                    ha1,
                    reqInfo["nonce"],
                    reqInfo["nc"],
                    reqInfo["cnonce"],
                    reqInfo["qop"],
                    ha2);
            }
            else
            {
                unhashedDigest = string.Format(
                    "{0}:{1}:{2}",
                    ha1,
                    reqInfo["nonce"],
                    ha2);
            }

            return unhashedDigest;
        }

        /// <summary>
        /// Creates MD5 hash.
        /// </summary>
        /// <param name="val">Passed unhashed string value.</param>
        /// <returns>Created hash.</returns>
        private string createMD5HashBinHex(string val)
        {
            byte[] ha1Bytes = MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(val));
            string ha1 = string.Empty;
            for (int i = 0; i < 16; i++)
            {
                ha1 += string.Format("{0:x02}", ha1Bytes[i]);
            }

            return ha1;
        }

        /// <summary>
        /// Check if current nonce is valid.
        /// </summary>
        /// <param name="nonce">Current nonce.</param>
        /// <returns>'true' if nonce is valid.</returns>
        private bool isNonceValid(string nonce)
        {
            DateTime expireTime;

            int numPadChars = nonce.Length % 4;
            if (numPadChars > 0)
            {
                numPadChars = 4 - numPadChars;
            }

            string newNonce = nonce.PadRight(nonce.Length + numPadChars, '=');

            try
            {
                byte[] decodedBytes = Convert.FromBase64String(newNonce);
                string expireStr = Encoding.ASCII.GetString(decodedBytes);
                expireTime = DateTime.Parse(expireStr);
            }
            catch (FormatException)
            {
                return false;
            }

            return DateTime.Now <= expireTime;
        }

        /// <summary>
        /// Retrieves user's password.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <returns>Users password.</returns>
        private string getUserPassword(string userName)
        {
            if(UserCollection.ContainsKey(userName))
            {
                return UserCollection[userName];
            }
            else
            {
                return string.Empty;
            }
        }
    }

    /// <summary>
    /// Class with Digest Authentication middleware extensions.
    /// </summary>
    public static class DigestAuthMiddlewareExtensions
    {
        /// <summary>
        /// Add Digest Authentication middleware.
        /// </summary>
        /// <param name="builder">The <see cref="IApplicationBuilder"/> instance.</param>
        /// <returns>The <see cref="IApplicationBuilder"/> instance.</returns>
        public static IApplicationBuilder UseDigestAuth(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DigestAuthMiddleware>();
        }
    }
}
