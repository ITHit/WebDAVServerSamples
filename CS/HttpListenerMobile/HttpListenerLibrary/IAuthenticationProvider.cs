using System.Collections.Specialized;
using System.Security.Principal;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Provides methods for authentication provider.
    /// </summary>
    public interface IAuthenticationProvider
    {
        /// <summary>
        /// Authenticates request.
        /// </summary>
        /// <param name="headers">Headers collection.</param>
        /// <param name="method">Http verb.</param>
        /// <returns>Authenticated <see cref="IPrincipal"/> instance.</returns>
        IPrincipal AuthenticateRequest(NameValueCollection headers, string method);

        /// <summary>
        /// Creates authentication header string.
        /// </summary>
        /// <returns>Authentication string</returns>
        string GetChallenge();
    }
}
