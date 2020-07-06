using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AzureDataLakeStorage.Configuration
{
    /// <summary>
    /// Represents WebDAV logger configuration.
    /// </summary>
    public class DavLoggerConfig
    {
        /// <summary>
        /// Defines whether debug logging mode is enabled.
        /// </summary>
        public bool IsDebugEnabled { get; set; } = false;

        /// <summary>
        /// Log file path. Make sure the application has enough permissions to create files in the folder
        /// where the log file is located - the application will rotate log files in this folder.
        /// In case you experience any issues with WebDAV, examine this log file first and search for exceptions and errors.
        /// </summary>
        public string LogFile { get; set; } = string.Empty;
    }

    /// <summary>
    /// Binds, validates and normalizes WebDAV Logger configuration.
    /// </summary>
    public static class DavLoggerConfigValidator
    {
        /// <summary>
        /// Binds, validates and normalizes WebDAV logger configuration.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="config">WebDAV Logger configuration.</param>
        /// <param name="env">Instance of <see cref="IWebHostEnvironment"/>.</param>
        public static async Task ReadConfigurationAsync(this IConfigurationSection configurationSection, DavLoggerConfig config, IWebHostEnvironment env)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }

            configurationSection.Bind(config);

            if (string.IsNullOrEmpty(config.LogFile))
            {
                throw new ArgumentNullException("Logger.LogFile");
            }

            if (!Path.IsPathRooted(config.LogFile))
            {
                config.LogFile = Path.GetFullPath(Path.Combine(env.ContentRootPath, config.LogFile));
            }

            // Create log folder and log file if does not exists.
            FileInfo logInfo = new FileInfo(config.LogFile);
            if (!logInfo.Exists)
            {
                if (!logInfo.Directory.Exists)
                {
                    logInfo.Directory.Create();
                }

                await using (FileStream stream = logInfo.Create()) { }
            }
        }
    }

}
