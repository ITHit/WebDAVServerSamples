using System.Collections.Generic;
using System.Threading.Tasks;

namespace SharedMobile
{
    /// <summary>
    /// Represents json configuration values collection and function to get file content from application bundle.
    /// </summary>
    public interface IConfigurationHelper
    {
        /// <summary>
        /// Represents collection of configuration values.
        /// </summary>
        IDictionary<string, string> JsonValuesCollection { get; }

        /// <summary>
        /// Retrieves file content by relative path from application bundle.
        /// </summary>
        Task<string> GetFileContentAsync(string filePath);
    }
}
