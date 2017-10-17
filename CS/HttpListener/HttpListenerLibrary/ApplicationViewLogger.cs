using System;
using ITHit.WebDAV.Server.Logger;
using HttpListenerLibrary.Options;
using Microsoft.Extensions.Options;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Performs logging on application view.
    /// </summary>
    public class ApplicationViewLogger : DefaultLoggerImpl
    {
        /// <summary>
        /// Logs messages to the view on the specific platform.
        /// </summary>
        private ILogMethod logMethod;

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="configOptions">Logger options from configuration.</param>
        /// <param name="logMethod">Represents view logging method.</param>
        public ApplicationViewLogger(IOptions<DavLoggerOptions> configOptions, ILogMethod logMethod)
        {
            this.logMethod = logMethod;

            DavLoggerOptions options = configOptions.Value;
            LogFile = options.LogFile;
            IsDebugEnabled = options.IsDebugEnabled;
        }

        /// <summary>
        /// Logs errors on application view and in file.
        /// </summary>
        /// <param name="message">Text to output.</param>
        /// <param name="exception">Exception instance.</param>
        public override void LogError(string message, Exception exception)
        {
            base.LogError(message, exception);
            logMethod.LogOutput($"{message}\n{exception?.StackTrace}");
        }
    }
}
