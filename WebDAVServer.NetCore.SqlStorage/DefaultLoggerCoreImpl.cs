using Microsoft.Extensions.Options;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Logger;
using WebDAVServer.NetCore.SqlStorage.Options;

namespace WebDAVServer.NetCore.SqlStorage
{
    public class DefaultLoggerCoreImpl : DefaultLoggerImpl, ILogger
    {
        /// <summary>
        /// Initializes new instance of the class <see cref="IOptions{LoggerOptions}"/> based on the specified <see cref="IOptions{LoggerOptions}"> instance.
        /// </summary>
        /// <param name="options"><see cref="IOptions{LoggerOptions}"/> instance.</param>
        public DefaultLoggerCoreImpl(IOptions<DavLoggerOptions> options)
            : this(options.Value) { }

        /// <summary>
        /// Initializes new instance of the class <see cref="DefaultLoggerCoreImpl"/> based on the specified <see cref="LoggerOptions"> instance.
        /// </summary>
        /// <param name="options"><see cref="LoggerOptions"/> instance.</param>
        public DefaultLoggerCoreImpl(DavLoggerOptions options)
        {
            LogFile = options.LogFile;
            IsDebugEnabled = options.IsDebugEnabled;
        }
    }
}
