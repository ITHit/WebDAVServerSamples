using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.Config
{
    /// <summary>
    /// Represents WebDAV Context configuration.
    /// </summary>
    public class DavContextConfig
    {
        /// <summary>
        /// Name of the Azure Storage Account used for the storage. You can get it from Azure Portal.
        /// </summary>
        public string AzureStorageAccountName { get; set; } = string.Empty;

        /// <summary>
        /// Access key for the Storage Account. You can get it from Azure Portal.
        /// </summary>
        public string AzureStorageAccessKey { get; set; } = string.Empty;

        /// <summary>
        /// That is name of the data lake storage container. Must be created first. You can create/get it from Azure Portal.
        /// </summary>
        public string DataLakeContainerName { get; set; } = string.Empty;

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
                throw new ArgumentNullException(nameof(configurationSection));
            }

            configurationSection.Bind(config);

            if (string.IsNullOrEmpty(config.DataLakeContainerName))
            {
                throw new ArgumentNullException("DavContextConfig.DataLakeContainerName");
            }

            if (string.IsNullOrEmpty(config.AzureStorageAccountName))
            {
                throw new ArgumentNullException("DavContextConfig.AzureStorageAccountName");
            }

            if (string.IsNullOrEmpty(config.AzureStorageAccessKey))
            {
                throw new ArgumentNullException("DavContextConfig.AzureStorageAccessKey");
            }
        }
    }
}
