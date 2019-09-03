
namespace WebDAVServer.FileSystemStorage.AspNetCore.Options
{
    /// <summary>
    /// Represents G Suite options.
    /// </summary>
    public class GSuiteOptions
    {
        /// <summary>
        /// Email of server google account
        /// </summary>
        public string ClientEmail { get; set; }

        /// <summary>
        /// Private key of server google account
        /// </summary>
        public string PrivateKey { get; set; }
       
    }
}
