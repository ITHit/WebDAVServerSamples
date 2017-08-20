using HttpListenerLibrary.Options;

namespace HttpListenerLibrary
{
    /// <summary>
    /// Represents configuration model definition.
    /// </summary>
    public class JsonConfigurationModel
    {
        /// <summary>
        /// Represents WebDAV Context configuration options.
        /// </summary>
        public DavContextOptions DavContextOptions { get; set; }

        /// <summary>
        /// Represents WebDAV Engine configuration options.
        /// </summary>
        public DavEngineOptions DavEngineOptions { get; set; }

        /// <summary>
        /// Represents WebDAV logger options.
        /// </summary>
        public DavLoggerOptions DavLoggerOptions { get; set; }

        /// <summary>
        /// Represents collection of user credentials.
        /// </summary>
        public DavUserOptions DavUsers { get; set; }
    }
}