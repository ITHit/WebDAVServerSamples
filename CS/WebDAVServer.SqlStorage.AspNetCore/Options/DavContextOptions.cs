using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
namespace WebDAVServer.SqlStorage.AspNetCore.Options
{
    /// <summary>
    /// Represents WebDAV Context configuration options.
    /// </summary>
    public class DavContextOptions
    {
        /// <summary>
        /// Database conntion string.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
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
        /// <param name="env">Instance of <see cref="IWebHostEnvironment"/>.</param>
        public static async Task ReadOptionsAsync(this IConfigurationSection configurationSection, DavContextOptions options, IWebHostEnvironment env)
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
