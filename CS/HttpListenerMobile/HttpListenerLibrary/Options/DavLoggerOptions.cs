
using System;

namespace HttpListenerLibrary.Options
{
    /// <summary>
    /// Represents WebDAV logger options.
    /// </summary>
    public class DavLoggerOptions
    {
        /// <summary>
        /// Defines whether debug logging mode is enabled.
        /// </summary>
        public bool IsDebugEnabled { get; set; }

        /// <summary>
        /// Log file path. Make sure the application has enough permissions to create files in the folder
        /// where the log file is located - the application will rotate log files in this folder.
        /// In case you experience any issues with WebDAV, examine this log file first and search for exceptions and errors.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// Function, which logs errors on user screen (application window in case of mobile devices or console in case of desktop).
        /// </summary>
        public Action<string> LogOutput { get; set; }
    }
}
