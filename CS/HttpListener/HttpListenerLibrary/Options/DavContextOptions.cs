using HttpListenerLibrary.ExtendedAttributes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Threading.Tasks;

namespace HttpListenerLibrary.Options
{
    /// <summary>
    /// Represents WebDAV Context configuration options.
    /// </summary>
    public class DavContextOptions
    {
        /// <summary>
        /// Files and folders in this folder become available via WebDAV.
        /// </summary>
        public string RepositoryPath { get; set; }

        /// <summary>
        /// Represents listener prefix, which will listen for requests.
        /// </summary>
        public string ListenerPrefix { get; set; }
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
        /// <param name="env">Instance of <see cref="IHostingEnvironment"/>.</param>
        public static async Task ReadOptionsAsync(this IConfigurationSection configurationSection, DavContextOptions options, IHostingEnvironment env)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }

            configurationSection.Bind(options);

            if (string.IsNullOrEmpty(options.ListenerPrefix))
            {
                throw new ArgumentNullException("DavContextOptions.ListenerPrefix");
            }

            if (string.IsNullOrEmpty(options.RepositoryPath))
            {
                throw new ArgumentNullException("DavContextOptions.RepositoryPath");
            }

            if (!Path.IsPathRooted(options.RepositoryPath))
            {
                options.RepositoryPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.RepositoryPath));
            }

            if (!Directory.Exists(options.RepositoryPath))
            {
                throw new DirectoryNotFoundException(string.Format("DavContextOptions.RepositoryPath specified in appsettings.webdav.json is invalid: '{0}'.", options.RepositoryPath));
            }

            if (!await new DirectoryInfo(options.RepositoryPath).IsExtendedAttributesSupportedAsync())
            {
                throw new NotSupportedException(string.Format("File system at '{0}' doesn't support extended attributes. This sample requires NTFS Alternate Data Streams support if running on Windows or extended attributes support if running on OS X or Linux.", options.RepositoryPath));
            }
        }
    }
}
