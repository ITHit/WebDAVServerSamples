using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Logger;
using System;
using System.Net;
using System.Security.Principal;
using System.Threading.Tasks;

namespace HttpListenerLibrary
{
    /// <summary>
    /// HttpListener implementation for WebDavServer.
    /// </summary>
    public class WebDAVHttpListener
    {
        /// <summary>
        /// If HttpListener is listening for incoming requests.
        /// </summary>
        public bool Listening { get; set; }

        /// <summary>
        /// IT Hit WebDav engine instance.
        /// </summary>
        private DavEngineAsync engine;

        /// <summary>
        /// Logger instance.
        /// </summary>
        private DefaultLoggerImpl logger;

        /// <summary>
        /// Contains HttpListener configuration.
        /// </summary>
        private JsonConfigurationModel configuration;

        /// <summary>
        /// Creates instance of this class.
        /// </summary>
        /// <param name="configuration">HttpListener configuration.</param>
        /// <param name="logger">Logger implementation instance.</param>
        public WebDAVHttpListener(
            JsonConfigurationModel configuration,
            DefaultLoggerImpl logger
            )
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        /// <summary>
        /// Initializes and starts HttpListener.
        /// </summary>
        public void RunListener()
        {
            try
            {
                Init();
                Listening = true;
                ThreadProcAsync();
            }
            catch (Exception ex)
            {
                string errorMessage = "Could not start listener. " + ex.Message;
                logger.LogError(errorMessage, ex);
            }
        }

        /// <summary>
        /// Performs HttpListener initialization.
        /// </summary>
        private void Init()
        {
            logger.LogFile = configuration.DavLoggerOptions.LogFile;
            logger.IsDebugEnabled = configuration.DavLoggerOptions.IsDebugEnabled;

            engine = new DavEngineAsync
            {
                Logger = logger,
                OutputXmlFormatting = true
            };

            engine.License = configuration.DavEngineOptions.License;
            MyCustomGetHandler handlerGet;
            MyCustomGetHandler handlerHead;

            if (configuration.DavContextOptions.GetFileContentFunc != null)
            {
                handlerGet = new MyCustomGetHandler(configuration.DavContextOptions.GetFileContentFunc);
                handlerHead = new MyCustomGetHandler(configuration.DavContextOptions.GetFileContentFunc);
            }
            else
            {
                handlerGet = new MyCustomGetHandler(configuration.DavContextOptions.HtmlPath);
                handlerHead = new MyCustomGetHandler(configuration.DavContextOptions.HtmlPath);
            }
            handlerGet.OriginalHandler = engine.RegisterMethodHandler("GET", handlerGet);
            handlerHead.OriginalHandler = engine.RegisterMethodHandler("HEAD", handlerHead);
        }

        /// <summary>
        /// Starting and listening for incoming requests.
        /// </summary>
        private async void ThreadProcAsync()
        {
            using (System.Net.HttpListener listener = new System.Net.HttpListener())
            {
                listener.Prefixes.Add(configuration.DavContextOptions.ListenerPrefix);

                listener.IgnoreWriteExceptions = true;

                listener.Start();

                while (Listening)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
#pragma warning disable 4014
                    Task.Factory.StartNew(() => ProcessRequestAsync(listener, context));
#pragma warning restore 4014
                }
            }
        }

        /// <summary>
        /// Process incoming request.
        /// </summary>
        /// <param name="listener"><see cref="HttpListener"/> instance.</param>
        /// <param name="context">HttpListener context instance.</param>
        /// <returns></returns>
        private async Task ProcessRequestAsync(System.Net.HttpListener listener, HttpListenerContext context)
        {
            try
            {
                IPrincipal principal = null;

                context.Response.SendChunked = false;
                context.Response.KeepAlive = false;

                var ntfsDavContext =
                    new DavContext(context, listener.Prefixes, principal, configuration.DavContextOptions.RepositoryPath, engine.Logger);

                await engine.RunAsync(ntfsDavContext);
            }
            finally
            {
                if (context != null && context.Response != null)
                {
                    try
                    {
                        context.Response.Close();
                    }
                    catch
                    {
                        // client closed connection before the content was sent
                    }
                }
            }
        }
    }
}
