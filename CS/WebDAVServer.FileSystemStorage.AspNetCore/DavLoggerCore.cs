using Microsoft.Extensions.Options;

using ITHit.WebDAV.Server.Logger;

using WebDAVServer.FileSystemStorage.AspNetCore.Options;

namespace WebDAVServer.FileSystemStorage.AspNetCore
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
        /// Initializes new instance of this class based on the WebDAV Logger configuration options.
        /// </summary>
        /// <param name="configOptions">WebDAV Logger configuration options.</param>
        public DavLoggerCore(IOptions<DavLoggerOptions> configOptions)
        {
            DavLoggerOptions options = configOptions.Value;
            LogFile         = options.LogFile;
            IsDebugEnabled  = options.IsDebugEnabled;
        }
    }
}
