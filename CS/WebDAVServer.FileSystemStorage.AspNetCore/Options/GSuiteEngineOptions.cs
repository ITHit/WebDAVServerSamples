
namespace WebDAVServer.FileSystemStorage.AspNetCore.Options
{
    /// <summary>
    /// Represents GSuite Server options.
    /// </summary>
    public class GSuiteEngineOptions : DavEngineOptions
    {
        /// <summary>
        /// Email of service account for Google Drive
        /// </summary>
        public string ServiceEmail { get; set; }

        /// <summary>
        /// Private key of service account for Google Drive
        /// </summary>
        public string ServicePrivateKey { get; set; }

    }
}
