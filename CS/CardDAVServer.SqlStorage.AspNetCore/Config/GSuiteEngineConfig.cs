
namespace CardDAVServer.SqlStorage.AspNetCore.Configuration
{
    /// <summary>
    /// Represents GSuite Server config.
    /// </summary>
    public class GSuiteEngineConfig : DavEngineConfig
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
