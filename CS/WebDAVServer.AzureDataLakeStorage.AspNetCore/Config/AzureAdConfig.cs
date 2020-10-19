using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebDAVServer.AzureDataLakeStorage.AspNetCore.Config
{
    /// <summary>
    /// Represents AzureAD configuration.
    /// </summary>
    public class AzureAdConfig
    {
        /// <summary>
        /// Authentication endpoint. Usually is set to https://login.microsoftonline.com/
        /// </summary>
        public string Instance { get; set; } = string.Empty;
        /// <summary>
        /// Tenant ID.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;
        /// <summary>
        /// Application (client) ID.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Binds, validates and normalizes WebDAV Engine configuration.
    /// </summary>
    public static class AzureAdConfigValidator
    {
        /// <summary>
        /// Binds, validates and normalizes AzureAD configuration.
        /// </summary>
        /// <param name="configurationSection">Instance of <see cref="IConfigurationSection"/>.</param>
        /// <param name="configuration">AzureAD configuration.</param>
        public static async Task ReadConfigurationAsync(this IConfigurationSection configurationSection,
            AzureAdConfig configuration)
        {
            if (configurationSection == null)
            {
                throw new ArgumentNullException("configurationSection");
            }

            configurationSection.Bind(configuration);
            if (string.IsNullOrEmpty(configuration.Instance))
            {
                throw new ArgumentNullException("AzureAD.Instance");
            }

            if (string.IsNullOrEmpty(configuration.TenantId))
            {
                throw new ArgumentNullException("AzureAD.TenantId");
            }

            if (string.IsNullOrEmpty(configuration.ClientId))
            {
                throw new ArgumentNullException("AzureAD.ClientId");
            }
        }
    }
}