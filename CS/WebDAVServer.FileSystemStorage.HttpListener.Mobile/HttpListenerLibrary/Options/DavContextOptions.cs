using System;
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
        /// Path to Html directory.
        /// </summary>
        public string HtmlPath { get; set; }

        /// <summary>
        /// Retrieves file content by path.
        /// </summary>
        public Func<string, Task<string>> GetFileContentFunc { get; set; }

        /// <summary>
        /// Represents listener prefix, which will listen for requests.
        /// </summary>
        public string ListenerPrefix { get; set; }
    }
}
