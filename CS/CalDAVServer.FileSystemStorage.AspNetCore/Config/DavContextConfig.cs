using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using CalDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;
namespace CalDAVServer.FileSystemStorage.AspNetCore.Configuration
{
    /// <summary>
    /// Represents WebDAV Context configuration.
    /// </summary>
    public class DavContextConfig
    {
        /// <summary>
        /// Files and folders in this folder become available via WebDAV.
        /// </summary>
        public string RepositoryPath { get; set; } = string.Empty;

        /// <summary>
        /// File system search provider. 
        /// </summary>
        public string WindowsSearchProvider { get; set; } = string.Empty;

        /// <summary>
        /// This folder used for extended attributes storage.
        /// </summary>
        public string AttrStoragePath { get; set; } = string.Empty;
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
            if (string.IsNullOrEmpty(config.RepositoryPath))
            {
                throw new ArgumentNullException("DavContext.RepositoryPath");
            }

            if (!Path.IsPathRooted(config.RepositoryPath))
            {
                config.RepositoryPath = Path.GetFullPath(Path.Combine(env.ContentRootPath, config.RepositoryPath));
            }

            if (!(string.IsNullOrEmpty(config.AttrStoragePath) || Path.IsPathRooted(config.AttrStoragePath)))
            {
                config.AttrStoragePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, config.AttrStoragePath));
            }

            if (!string.IsNullOrEmpty(config.AttrStoragePath))
            {
                FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(config.AttrStoragePath, config.RepositoryPath));
            }
        }
    }
}
