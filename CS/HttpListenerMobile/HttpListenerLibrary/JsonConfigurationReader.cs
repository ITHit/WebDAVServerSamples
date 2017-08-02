using Newtonsoft.Json;
using System;
using System.IO;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Performs reading and validating logic on configuration instance.
    /// </summary>
    public class JsonConfigurationReader
    {
        /// <summary>
        /// Reads configuration from file.
        /// </summary>
        /// <param name="jsonPath">Path to specified file.</param>
        /// <returns>Configutation model instance.</returns>
        public static JsonConfigurationModel ReadConfiguration(string jsonPath)
        {
            if(!File.Exists(jsonPath))
            {
                throw new ArgumentException($"Configuration file with path {jsonPath} does not exist.");
            }
            return JsonConvert.DeserializeObject<JsonConfigurationModel>(File.ReadAllText(jsonPath));
        }

        /// <summary>
        /// Reads configuration from stream.
        /// </summary>
        /// <param name="stream">Configuration stream.</param>
        /// <returns>Configutation model instance.</returns>
        public static JsonConfigurationModel ReadConfiguration(Stream stream)
        {
            if(stream == null)
            {
                throw new ArgumentException("Empty stream.");
            }
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader streamReader = new StreamReader(stream))
            {
                using (JsonTextReader jsonTextReader = new JsonTextReader(streamReader))
                {
                    return serializer.Deserialize<JsonConfigurationModel>(jsonTextReader);
                }
            }
        }

        /// <summary>
        /// Validates and fixes configuration values.
        /// </summary>
        /// <param name="configurationModel">Configutation model instance.</param>
        /// <param name="contentRootPath">Root content path.</param>
        public static void ValidateConfiguration(JsonConfigurationModel configurationModel, string contentRootPath)
        {
            if (string.IsNullOrEmpty(configurationModel.DavContextOptions.RepositoryPath))
            {
                throw new Exception("Invalid RepositoryPath configuration parameter value.");
            }

            if (string.IsNullOrEmpty(configurationModel.DavContextOptions.ListenerPrefix))
            {
                throw new Exception("ListenerPrefix section is missing or invalid!");
            }

            configurationModel.DavContextOptions.RepositoryPath = Path.Combine(contentRootPath, configurationModel.DavContextOptions.RepositoryPath);
            if (!Directory.Exists(configurationModel.DavContextOptions.RepositoryPath))
            {
                throw new FileNotFoundException("Storage folder hasn't been found.");
            }

            if(!string.IsNullOrEmpty(configurationModel.DavLoggerOptions.LogFile))
            {
                configurationModel.DavLoggerOptions.LogFile = Path.Combine(contentRootPath, configurationModel.DavLoggerOptions.LogFile);
            }
        }
    }
}
