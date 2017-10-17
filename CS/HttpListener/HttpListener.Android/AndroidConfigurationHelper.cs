using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using ITHit.WebDAV.Server;
using Android.Content.Res;
using SharedMobile;

namespace HttpListener.Android
{
    /// <summary>
    /// Represents json configuration values collection and function to get file content from android assets.
    /// </summary>
    public class AndroidConfigurationHelper : IConfigurationHelper
    {
        /// <summary>
        /// <see cref="AssetManager"/> instance.
        /// </summary>
        private AssetManager assetManager;

        /// <summary>
        /// Json configuration file name.
        /// </summary>
        private string jsonFileName;

        /// <summary>
        /// Represents collection of configuration values.
        /// </summary>
        private IDictionary<string, string> jsonValueCollection;

        /// <summary>
        /// Represents access to the configuration values.
        /// </summary>
        public IDictionary<string, string> JsonValuesCollection
        {
            get
            {
                if(jsonValueCollection == null)
                {
                    jsonValueCollection = new JsonConfigurationFileParser().Parse(assetManager.Open(jsonFileName));
                }
                return jsonValueCollection;
            }
        }

        /// <summary>
        /// Creates new instance of this class.
        /// </summary>
        /// <param name="assetManager"><see cref="AssetManager"/> instance.</param>
        /// <param name="jsonFileName">Json configuration file name.</param>
        public AndroidConfigurationHelper(AssetManager assetManager, string jsonFileName)
        {
            this.assetManager = assetManager;
            this.jsonFileName = jsonFileName;
        }

        /// <summary>
        /// Retrieves file content by relative file path in Assets.
        /// </summary>
        /// <param name="filePath">Relative file path in Assets.</param>
        /// <returns>File content in string representation.</returns>
        /// <exception cref="DavException">If file with specified path does not exist.</exception>
        public async Task<string> GetFileContentAsync(string filePath)
        {
            try
            {
                using (StreamReader streamReader = new StreamReader(assetManager.Open(filePath)))
                {
                    return await streamReader.ReadToEndAsync();
                }
            }
            catch (Java.IO.FileNotFoundException exception)
            {
                throw new DavException("File not found in assets: " + filePath, exception, DavStatus.NOT_FOUND);
            }
        }
    }
}