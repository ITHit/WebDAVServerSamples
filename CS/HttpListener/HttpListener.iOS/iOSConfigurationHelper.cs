using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using ITHit.WebDAV.Server;
using SharedMobile;

namespace HttpListener.iOS
{
    /// <summary>
    /// Represents json configuration values collection and function to get file content from iOS bundle.
    /// </summary>
    public class iOSConfigurationHelper : IConfigurationHelper
    {
        /// <summary>
        /// Content root path.
        /// </summary>
        private string contentRootPath;

        /// <summary>
        /// Json configuration file name.
        /// </summary>
        private string jsonFileName;

        /// <summary>
        /// Represents collection of configuration values.
        /// </summary>
        private IDictionary<string, string> jsonValuesCollection;

        /// <summary>
        /// Represents access to the configuration values.
        /// </summary>
        public IDictionary<string, string> JsonValuesCollection
        {
            get
            {
                if (jsonValuesCollection == null)
                {
                    jsonValuesCollection = new JsonConfigurationFileParser().Parse(File.OpenRead(Path.Combine(contentRootPath, jsonFileName)));
                }
                return jsonValuesCollection;
            }
        }

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="contentRootPath">Content root path.</param>
        /// <param name="jsonFileName">Json configuration file name.</param>
        public iOSConfigurationHelper(string contentRootPath, string jsonFileName)
        {
            this.contentRootPath = contentRootPath;
            this.jsonFileName = jsonFileName;
        }

        /// <summary>
        /// Retrieves file content by relative file path.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>File content in string representation.</returns>
        /// <exception cref="DavException">If file with specified path does not exist.</exception>
        public async Task<string> GetFileContentAsync(string filePath)
        {
            string fullFilePath = Path.Combine(contentRootPath, filePath);
            if (!File.Exists(fullFilePath))
            {
                throw new DavException("File not found: " + fullFilePath, DavStatus.NOT_FOUND);
            }

            using (TextReader reader = File.OpenText(fullFilePath))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}