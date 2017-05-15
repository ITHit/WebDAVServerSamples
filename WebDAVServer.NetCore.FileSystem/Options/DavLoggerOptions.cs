using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WebDAVServer.NetCore.FileSystem.Options
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
    }

    /// <summary>
    /// Binds, validates and normalizes WebDAV Logger configuration options.
    /// </summary>
    public static class DavLoggerOptionsValidator
    {
        /// <summary>
        /// Binds, validates and normalizes WebDAV logger options.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="options">WebDAV Logger configuration options.</param>
        public static async Task ReadOptionsAsync(this IConfigurationSection configurationSection, DavLoggerOptions options, IHostingEnvironment env)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }

            configurationSection.Bind(options);

            if (string.IsNullOrEmpty(options.LogFile))
            {
                throw new ArgumentNullException("LoggerOptions.LogFile");
            }

            if (!Path.IsPathRooted(options.LogFile))
            {
                options.LogFile = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.LogFile));
            }

            // Create log folder and log file if does not exists.
            FileInfo logInfo = new FileInfo(options.LogFile);
            if (!logInfo.Exists)
            {
                if (!logInfo.Directory.Exists)
                {
                    logInfo.Directory.Create();
                }

                using (FileStream stream = logInfo.Create()) { }
            }
        }
    }

}
