using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CalDAVServer.FileSystemStorage.AspNetCore.Configuration
{

    /// <summary>
    /// Represents WebDAV Engine configuration.
    /// </summary>
    public class DavEngineConfig
    {

        /// <summary>
        /// Specifies whether XML written to the output will be formatted. Default is <b>false</b>.
        /// </summary>
        public bool OutputXmlFormatting { get; set; } = false;

        /// <summary>
        /// Specifies whether engine shall use full or relative urls. Default is <b>true</b>.
        /// </summary>
        /// <remarks>
        /// By default full urls are used.
        /// </remarks>
        public bool UseFullUris { get; set; } = true;

        /// <summary>
        /// Enables or disables CORS for specified domain. If "*" is specified, CORS will be enabled for in all domains.
        /// </summary>
        public string CorsAllowedFor { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the license text. See comments in appsettings.webdav.json where to get the license file.
        /// </summary>
        public string License { get; set; } = string.Empty;
    }

    /// <summary>
    /// Binds, validates and normalizes WebDAV Engine configuration.
    /// </summary>
    public static class DavEngineConfigValidator
    {
        /// <summary>
        /// Binds, validates and normalizes WebDAV Engine configuration.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="configuration">WebDAV Engine configuration.</param>
        public static async Task ReadConfigurationAsync(this IConfigurationSection configurationSection, DavEngineConfig configuration)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }
            configurationSection.Bind(configuration);
        }
    }
}
