using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.GSuite.Server;

using WebDAVServer.FileSystemStorage.AspNetCore.Cookies.Configuration;


namespace WebDAVServer.FileSystemStorage.AspNetCore.Cookies
{
    /// <summary>
    /// Represents GSuite Engine. Processes GSuite requests and generates responses. 
    /// Provides constructors specific for ASP.NET Core implementation, that can read configuration parameters.
    /// </summary>
    /// <remarks>
    /// A single instance of this class per application is created.
    /// </remarks>
    public class GSuiteEngineCore : GSuiteEngineAsync
    {
        /// <summary>
        /// Initializes new instance of this class based on the GSuite Engine configuration and logger instance.
        /// </summary>
        /// <param name="gSuiteConf">GSuite Engine configuration.</param>
        /// <param name="logger">Logger instance.</param>
        public GSuiteEngineCore(IOptions<GSuiteEngineConfig> gSuiteConf,
            ILogger logger) : base(gSuiteConf.Value.GoogleServiceAccountID, gSuiteConf.Value.GoogleServicePrivateKey, 
                                   gSuiteConf.Value.GoogleNotificationsRelativeUrl)
        {
            GSuiteEngineConfig config = gSuiteConf.Value;

            OutputXmlFormatting = config.OutputXmlFormatting;
            CorsAllowedFor = config.CorsAllowedFor;
            License = config.License;
            Logger = logger;
        }
    }
}
