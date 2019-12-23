using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.GSuite.Server;

using WebDAVServer.FileSystemStorage.AspNetCore.Options;


namespace WebDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Represents WebDAV Engine. Processes WebDAV requests and generates responses. 
    /// Provides constructors specific for ASP.NET Core implementation, that can read configuration parameters.
    /// </summary>
    /// <remarks>
    /// A single instance of this class per application is created.
    /// </remarks>
    public class GSuiteEngineCore : GSuiteEngineAsync
    {
        /// <summary>
        /// Initializes new instance of this class based on the WebDAV Engine configuration options and logger instance.
        /// </summary>
        /// <param name="gSuiteOptions">GSuite Engine configuration options.</param>
        /// <param name="logger">Logger instance.</param>
        public GSuiteEngineCore(IOptions<GSuiteEngineOptions> gSuiteOptions,
            ILogger logger) : base(gSuiteOptions.Value.GoogleServiceAccountID, gSuiteOptions.Value.GoogleServicePrivateKey)
        {
            GSuiteEngineOptions options = gSuiteOptions.Value;

            OutputXmlFormatting = options.OutputXmlFormatting;
            CorsAllowedFor = options.CorsAllowedFor;
            License = options.License;
            Logger = logger;
        }
    }
}
