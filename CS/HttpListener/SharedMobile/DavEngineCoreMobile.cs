using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

using ITHit.WebDAV.Server;
using HttpListenerLibrary.Options;
using HttpListenerLibrary;

namespace SharedMobile
{
    /// <summary>
    /// Represents WebDAV Engine specific for mobile platforms. Processes WebDAV requests and generates responses. 
    /// </summary>
    /// <remarks>
    /// A single instance of this class per application is created.
    /// </remarks>
    public class DavEngineCoreMobile : DavEngineAsync
    {
        /// <summary>
        /// Initializes new instance of this class based on the WebDAV Engine configuration options and logger instance.
        /// </summary>
        /// <param name="configOptions">WebDAV Engine configuration options.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="env">IHostingEnvironment instance.</param>
        /// <param name="configurationHelper">Represents function to get file content from application bundle.</param>
        public DavEngineCoreMobile(IOptions<DavEngineOptions> configOptions, ILogger logger, IHostingEnvironment env, IConfigurationHelper configurationHelper) : base()
        {
            DavEngineOptions options = configOptions.Value;

            OutputXmlFormatting         = options.OutputXmlFormatting; 
            UseFullUris                 = options.UseFullUris;
            CorsAllowedFor              = options.CorsAllowedFor;
            License                     = options.License;

            Logger                      = logger;

            // Set custom handler to process GET and HEAD requests to folders and display 
            // info about how to connect to server. We are using the same custom handler 
            // class (but different instances) here to process both GET and HEAD because 
            // these requests are very similar. 
            // Note that some WebDAV clients may fail to connect if HEAD request is not processed.
            MyCustomGetHandler handlerGet = new MyCustomGetHandler(configurationHelper.GetFileContentAsync);
            MyCustomGetHandler handlerHead = new MyCustomGetHandler(configurationHelper.GetFileContentAsync);
            handlerGet.OriginalHandler = RegisterMethodHandler("GET", handlerGet);
            handlerHead.OriginalHandler = RegisterMethodHandler("HEAD", handlerHead);
        }
    }
}
