using Microsoft.Extensions.Options;

using ITHit.WebDAV.Server.Logger;

using WebDAVServer.AzureDataLakeStorage.AspNetCore.Configuration;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore
{
    /// <summary>
    /// Represents WebDAV Logger. Logs WebDAV requests, responses and errors. 
    /// Provides constructors specific for ASP.NET Core implementation, that can read configuration parameters.
    /// </summary>
    /// <remarks>
    /// A single instance of this class per application is created.
    /// </remarks>
    public class DavLoggerCore : DefaultLoggerImpl
    {
        /// <summary>
        /// Initializes new instance of this class based on the WebDAV Logger configuration.
        /// </summary>
        /// <param name="configOptions">WebDAV Logger configuration.</param>
        public DavLoggerCore(IOptions<DavLoggerConfig> configOptions)
        {
            DavLoggerConfig loggerConfig = configOptions.Value;
            LogFile         = loggerConfig.LogFile;
            IsDebugEnabled  = loggerConfig.IsDebugEnabled;
        }
    }
}
