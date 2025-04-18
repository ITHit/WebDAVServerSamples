using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ILogger = ITHit.Server.ILogger;

using CalDAVServer.FileSystemStorage.AspNetCore.Configuration;

namespace CalDAVServer.FileSystemStorage.AspNetCore
{
    /// <summary>
    /// Represents WebDAV Engine. Processes WebDAV requests and generates responses. 
    /// Provides constructors specific for ASP.NET Core implementation, that can read configuration parameters.
    /// </summary>
    /// <remarks>
    /// A single instance of this class per application is created.
    /// </remarks>
    public class DavEngineCore : DavEngineAsync
    {
        /// <summary>
        /// Initializes new instance of this class based on the WebDAV Engine configuration options and logger instance.
        /// </summary>
        /// <param name="config">WebDAV Engine configuration.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="env">IWebHostEnvironment instance.</param>
        public DavEngineCore(IOptions<DavEngineConfig> config, ILogger logger, IWebHostEnvironment env) : base()
        {
            DavEngineConfig engineConfig = config.Value;

            OutputXmlFormatting         = engineConfig.OutputXmlFormatting; 
            UseFullUris                 = engineConfig.UseFullUris;
            CorsAllowedFor              = engineConfig.CorsAllowedFor;
            License                     = engineConfig.License;

            Logger = logger;

            // Set custom handler to process GET and HEAD requests to folders and display 
            // info about how to connect to server. We are using the same custom handler 
            // class (but different instances) here to process both GET and HEAD because 
            // these requests are very similar. 
            // Note that some WebDAV clients may fail to connect if HEAD request is not processed.
            MyCustomGetHandler handlerGet = new MyCustomGetHandler(env.ContentRootPath);
            MyCustomGetHandler handlerHead = new MyCustomGetHandler(env.ContentRootPath);
            handlerGet.OriginalHandler = RegisterMethodHandler("GET", handlerGet);
            handlerHead.OriginalHandler = RegisterMethodHandler("HEAD", handlerHead);
   
            // Set your iCalendar & vCard library license before calling any members.
            // iCalendar & vCard library accepts:
            // - WebDAV Server Engine license with iCalendar & vCard modules. Verify your license file to see if these modules are specified.
            // - or iCalendar and vCard Library license.
            ITHit.Collab.LicenseValidator.SetLicense(config.Value.License);
        }
    }
}
