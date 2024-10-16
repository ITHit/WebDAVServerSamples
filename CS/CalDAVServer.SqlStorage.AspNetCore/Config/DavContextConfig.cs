using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
namespace CalDAVServer.SqlStorage.AspNetCore.Configuration
{
    /// <summary>
    /// Represents WebDAV Context configuration.
    /// </summary>
    public class DavContextConfig
    {

        /// <summary>
        /// Database conntion string.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// Binds, validates and normalizes WebDAV Context configuration.
    /// </summary>
    public static class DavContextConfigValidator
    {
        /// <summary>
        /// Binds, validates and normalizes WebDAV Context configuration.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="config">WebDAV Context configuration.</param>
        /// <param name="env">Instance of <see cref="IWebHostEnvironment"/>.</param>
        public static async Task ReadConfigurationAsync(this IConfigurationSection configurationSection, DavContextConfig config, IWebHostEnvironment env)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }

            configurationSection.Bind(config);
            if (string.IsNullOrEmpty(config.ConnectionString))
            {
                throw new ArgumentNullException("DavContext.ConnectionString");
            }
        }
    }
}
