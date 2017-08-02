using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace HttpListenerLibrary.Options
{

    /// <summary>
    /// Represents WebDAV Engine configuration options.
    /// </summary>
    public class DavEngineOptions
    {

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
