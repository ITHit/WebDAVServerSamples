using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.Config
{
    /// <summary>
    /// Represents Azure Cognitive Search config.
    /// </summary>
    public class SearchConfig
    {
        /// <summary>
        /// Name of Azure Cognitive Search service name.
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Name of Azure Cognitive Search index name.
        /// </summary>
        public string IndexName { get; set; } = string.Empty;

        /// <summary>
        /// Cognitive search API key.
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

    }

    /// <summary>
    /// Binds, validates and normalizes Cognitive Search configuration.
    /// </summary>
    public static class SearchConfigValidator
    {
        /// <summary>
        /// Binds, validates and normalizes Cognitive Search configuration.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="config">Cognitive Search configuration.</param>
        /// <param name="env">Instance of <see cref="IWebHostEnvironment"/>.</param>
        public static async Task ReadConfigurationAsync(this IConfigurationSection configurationSection, SearchConfig config, IWebHostEnvironment env)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException(nameof(configurationSection));
            }

            configurationSection.Bind(config);
        }
    }
}
