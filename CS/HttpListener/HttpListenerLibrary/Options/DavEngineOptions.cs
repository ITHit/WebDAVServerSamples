using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace HttpListenerLibrary.Options
{

    /// <summary>
    /// Represents WebDAV Engine configuration options.
    /// </summary>
    public class DavEngineOptions
    {
        /// <summary>
        /// Specifies whether XML written to the output will be formatted. Default is <b>false</b>.
        /// </summary>
        public bool OutputXmlFormatting { get; set; }

        /// <summary>
        /// Specifies whether engine shall use full or relative urls. Default is <b>true</b>.
        /// </summary>
        /// <remarks>
        /// By default full urls are used.
        /// </remarks>
        public bool UseFullUris { get; set; }

        /// <summary>
        /// Enables or disables CORS for specified domain. If "*" is specified, CORS will be enabled for in all domains.
        /// </summary>
        public string CorsAllowedFor { get; set; }

        /// <summary>
        /// Gets or sets the license text. See comments in appsettings.webdav.json where to get the license file.
        /// </summary>
        public string License { get; set; }
    }

    /// <summary>
    /// Binds, validates and normalizes WebDAV Engine configuration options.
    /// </summary>
    public static class DavEngineOptionsValidator
    {
        /// <summary>
        /// Binds, validates and normalizes WebDAV Engine configuration options.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="options">WebDAV Engine configuration options.</param>
        public static async Task ReadOptionsAsync(this IConfigurationSection configurationSection, DavEngineOptions options)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }
            configurationSection.Bind(options);
        }
    }
}
