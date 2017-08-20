using System;
using ITHit.WebDAV.Server.Logger;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Performs logging on application view.
    /// </summary>
    public class ApplicationViewLogger : DefaultLoggerImpl
    {
        /// <summary>
        /// Function, which outputs message on user mobile screen.
        /// </summary>
        public Action<string> LogOutput { get; set; }

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="logFunction">Screen logging function.</param>
        public ApplicationViewLogger(Action<string> logFunction)
        {
            LogOutput = logFunction;
        }

        /// <summary>
        /// Logs errors on application view and in file.
        /// </summary>
        /// <param name="message">Text to output.</param>
        /// <param name="exception">Exception instance.</param>
        public override void LogError(string message, Exception exception)
        {
            base.LogError(message, exception);
            LogOutput($"{message}\n{exception?.StackTrace}");
        }

        /// <summary>
        /// Logs message on application view.
        /// </summary>
        /// <param name="message">Text to output.</param>
        public void LogMessage(string message)
        {
            LogOutput(message);
        }
    }
}
