using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using WebDAVServer.FileSystemStorage.AspNetCore.ExtendedAttributes;
namespace WebDAVServer.FileSystemStorage.AspNetCore.Options
{
    /// <summary>
    /// Represents WebDAV Context configuration options.
    /// </summary>
    public class DavContextOptions
    {
        /// <summary>
        /// Files and folders in this folder become available via WebDAV.
        /// </summary>
        public string RepositoryPath { get; set; } = string.Empty;

        /// <summary>
        /// This folder used for extended attributes storage.
        /// </summary>
        public string AttrStoragePath { get; set; } = string.Empty;
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

            if (!(string.IsNullOrEmpty(options.AttrStoragePath) || Path.IsPathRooted(options.AttrStoragePath)))
            {
                options.AttrStoragePath = Path.GetFullPath(Path.Combine(env.ContentRootPath, options.AttrStoragePath));
            }

            if (!string.IsNullOrEmpty(options.AttrStoragePath))
            {
                FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(options.AttrStoragePath, options.RepositoryPath));
            }
            else if (!await new DirectoryInfo(options.RepositoryPath).IsExtendedAttributesSupportedAsync())
            {
                var tempPath = Path.Combine(Path.GetTempPath(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(tempPath, options.RepositoryPath));
            }
        }
    }
}
