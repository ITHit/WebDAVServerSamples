using System;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace WebDAVServer.NetCore.SqlStorage.Options
{
    /// <summary>
    /// Represents WebDAV Context configuration options.
    /// </summary>
    public class DavContextOptions
    {
        /// <summary>
        /// Database conntion string.
        /// </summary>
        public string ConnectionString { get; set; }
    }


    /// <summary>
    /// Binds, validates and normalizes WebDAV Context configuration options.
    /// </summary>
    public static class DavContextOptionsValidator
    {
        /// <summary>
        /// Binds, validates and normalizes WebDAV Context configuration options.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="options">WebDAV Context configuration options.</param>
        public static async Task ReadOptionsAsync(this IConfigurationSection configurationSection, DavContextOptions options)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }

            configurationSection.Bind(options);

            if (string.IsNullOrEmpty(options.ConnectionString))
            {
                throw new ArgumentNullException("DavContextOptions.ConnectionString");
            }
        }
    }
}
