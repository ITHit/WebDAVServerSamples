
namespace WebDAVServer.FileSystemStorage.AspNetCore.Options
{
    /// <summary>
    /// Represents GSuite Server options.
    /// </summary>
    public class GSuiteEngineOptions : DavEngineOptions
    {
        /// <summary>
        /// Google Service Account ID (client_email field from JSON file).
        /// </summary>
        public string GoogleServiceAccountID { get; set; }

        /// <summary>
        /// Google Service private key (private_key field from JSON file).
        /// </summary>
        public string GoogleServicePrivateKey { get; set; }

    }
}
