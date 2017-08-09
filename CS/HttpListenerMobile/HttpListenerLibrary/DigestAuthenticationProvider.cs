using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Contains Digest authentication logic, so it can be reused in ASP.NET digest authentication module
    /// as well as with HTTP listener.
    /// </summary>
    public class DigestAuthenticationProvider : IAuthenticationProvider
    {
        private bool isNonceStale = false;
        private const string realm = "ITHitWebDAVServer";
        private readonly Func<string, PasswordAndRoles> getPasswordAndRolesByUsernameFunction;

        /// <summary>
        /// Initializes a new instance of the DigestAuthenticationProvider class.
        /// </summary>
        /// <param name="getPasswordAndRolesByUsernameFunction">Function which can be used to retrieve user's
        /// password and roles by his name.</param>
        public DigestAuthenticationProvider(Func<string, PasswordAndRoles> getPasswordAndRolesByUsernameFunction)
        {
            this.getPasswordAndRolesByUsernameFunction = getPasswordAndRolesByUsernameFunction;
        }

        /// <summary>
        /// Whether authorization header is present in headers collection.
        /// </summary>
        /// <param name="headers">Collection with headers.</param>
        /// <returns>'true' if required header is present. 'false' otherwise.</returns>
        public bool IsAuthorizationPresent(NameValueCollection headers)
        {
            string authStr = headers["Authorization"];
            return authStr != null && authStr.ToLower().StartsWith("digest");
        }

        /// <summary>
        /// Authenticates request.
        /// </summary>
        /// <param name="headers">Headers collection.</param>
        /// <param name="method">Http verb.</param>
        /// <returns>Authenticated <see cref="IPrincipal"/> instance or <c>null</c> otherwise.</returns>
        public IPrincipal AuthenticateRequest(NameValueCollection headers, string method)
        {
            if (!IsAuthorizationPresent(headers))
            {
                return null;
            }

            string authStr = headers["Authorization"];

            authStr = authStr.Trim().Substring(7);

            //Filling header segments in dictionary.
            Dictionary<string, string> reqInfo = new Dictionary<string, string>();
            MatchCollection matches = Regex.Matches(authStr, "([a-zA-Z0-9]+)=(([^\",]+)|(\\\"([^\"]*)\\\")),?");
            // Group 1 - Param name
            // Group 2 - Whole value string
            // Group 3 - Value if not in "\"" e.g. alg=MD5
            // Group 5 - Value if in "\"" e.g. name="name"
            foreach (Match match in matches)
            {
                string value = match.Groups[5].Value;
                if (value.Length == 0)
                {
                    value = match.Groups[3].Value;
                }
                reqInfo.Add(match.Groups[1].Value, value);
            }

            string clientUsername = reqInfo.ContainsKey("username") ? reqInfo["username"] : string.Empty;

            // workaround for Windows Vista Digest Authorization. User name may be submitted in the following format:
            // Machine\\User.
            clientUsername = clientUsername.Replace("\\\\", "\\");
            reqInfo["username"] = clientUsername;
            string username = clientUsername;
            int ind = username.LastIndexOf('\\');
            if (ind > 0)
            {
                username = username.Remove(0, ind + 1);
            }

            PasswordAndRoles par = getPasswordAndRolesByUsernameFunction(username);
            if (par == null)
            {
                return null;
            }

            string unhashedDigest = generateUnhashedDigest(par.Password, reqInfo, method);
            string hashedDigest = createMD5HashBinHex(unhashedDigest);

            isNonceStale = !isNonceValid(reqInfo["nonce"]);

            if ((reqInfo["response"] != hashedDigest) || isNonceStale)
            {
                return null;
            }

            return new GenericPrincipal(new GenericIdentity(clientUsername, "digest"), par.Roles);
        }

        public string GetChallenge()
        {
            string nonce = createNewNonce();

            StringBuilder stringBuilder = new StringBuilder("Digest");
            stringBuilder.Append(" realm=\"");
            stringBuilder.Append(realm);
            stringBuilder.Append("\", nonce=\"");
            stringBuilder.Append(nonce);
            stringBuilder.Append("\", opaque=\"0000000000000000\", stale=");
            stringBuilder.Append(isNonceStale ? "true" : "false");
            stringBuilder.Append(", algorithm=MD5, qop=\"auth\"");
            return stringBuilder.ToString();
        }

        private static string generateUnhashedDigest(
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

        private static string createMD5HashBinHex(string val)
        {
            byte[] ha1Bytes = new MD5CryptoServiceProvider().ComputeHash(Encoding.ASCII.GetBytes(val)); // if MD5CryptoServiceProvider is created as static member 'handle is invalid' is thrown some times in case of HttpListener
            string ha1 = string.Empty;
            for (int i = 0; i < 16; i++)
            {
                ha1 += string.Format("{0:x02}", ha1Bytes[i]);
            }

            return ha1;
        }

        private static string createNewNonce()
        {
            DateTime nonceTime = DateTime.Now + TimeSpan.FromMinutes(1);
            string expireStr = nonceTime.ToString("G");

            byte[] expireBytes = Encoding.ASCII.GetBytes(expireStr);
            string nonce = Convert.ToBase64String(expireBytes);

            nonce = nonce.TrimEnd(new char[] { '=' });
            return nonce;
        }

        private static bool isNonceValid(string nonce)
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
        /// User password and roles.
        /// </summary>
        public class PasswordAndRoles
        {
            /// <summary>
            /// Initializes a new instance of the PasswordAndRoles class.
            /// </summary>
            /// <param name="password">Password of a user.</param>
            /// <param name="roles">Roles user belongs to.</param>
            public PasswordAndRoles(string password, string[] roles)
            {
                this.Password = password;
                this.Roles = roles;
            }

            /// <summary>
            /// Gets password of a user.
            /// </summary>
            public string Password { get; private set; }

            /// <summary>
            /// Gets roles user belongs to.
            /// </summary>
            public string[] Roles { get; private set; }
        }
    }
}
