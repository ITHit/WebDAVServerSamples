using System;
using System.Configuration;
using System.IO;
using System.Web;

using ITHit.Server;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Logger;

namespace CalDAVServer.FileSystemStorage.AspNet
{
    /// <summary>
    /// Logger singleton.
    /// We need this singleton because logging is used in various modules, like authentication etc.
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Whether debug logging is enabled. In particular most request/response bodies will
        /// be logged if debug logging is on.
        /// </summary>
        private static readonly bool debugLoggingEnabled =
            "true".Equals(
                ConfigurationManager.AppSettings["DebugLoggingEnabled"],
                StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Path where log files will be stored.
        /// </summary>
        private static readonly string logPath = ConfigurationManager.AppSettings["LogPath"];

        /// <summary>
        /// Synchronization object.
        /// </summary>
        private static readonly object syncRoot = new object();

        /// <summary>
        /// Gets logger instace.
        /// </summary>
        public static ILogger Instance
        {
            get
            {
                var context = HttpContext.Current;
                const string LOGGER_KEY = "$DavLogger$";
                if (context.Application[LOGGER_KEY] == null)
                {
                    lock (syncRoot)
                    {
                        if (context.Application[LOGGER_KEY] == null)
                        {
                            context.Application[LOGGER_KEY] = initLogger();
                        }
                    }
                }

                return (ILogger)context.Application[LOGGER_KEY];
            }
        }

        /// <summary>
        /// Initializes logger.
        /// </summary>
        /// <returns>Instance of <see cref="ILogger"/>.</returns>
        private static ILogger initLogger()
        {
            var logger = new DefaultLoggerImpl();
            var context = HttpContext.Current;

            if (!string.IsNullOrEmpty(logPath))
            {
                logger.LogFile = Path.Combine(context.Server.MapPath(logPath), "WebDAVlog.txt");
            }
            else
            {
                logger.LogFile = Path.Combine(context.Request.PhysicalApplicationPath, "WebDAVlog.txt");
            }

            logger.IsDebugEnabled = debugLoggingEnabled;

            return logger;
        }
    }
}
