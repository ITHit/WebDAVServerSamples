using HttpListenerLibrary.Options;
using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Logger;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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
        /// Authentication provider instance.
        /// </summary>
        private DigestAuthenticationProvider digestProvider;

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
            digestProvider = new DigestAuthenticationProvider(GetPasswordAndRoles);
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
                configuration.DavLoggerOptions.LogOutput(ex.Message);
                configuration.DavLoggerOptions.LogOutput(ex.StackTrace);
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
            try
            {
                using (System.Net.HttpListener listener = new System.Net.HttpListener())
                {
                    listener.Prefixes.Add(configuration.DavContextOptions.ListenerPrefix);

                    // We do not use AuthenticationSchemes.Digest here because OPTIONS request must be processed without authentication. 
                    // Instead this sample provides its own Digest authentication implementation.
                    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

                    listener.IgnoreWriteExceptions = true;

                    listener.Start();

                    string listenerPrefix = configuration.DavContextOptions.ListenerPrefix.Replace("+", LocalIPAddress().ToString());
                    configuration.DavLoggerOptions.LogOutput($"Started listening on {configuration.DavContextOptions.ListenerPrefix}.\n\n" +
                        $"To access your files go to {listenerPrefix} in a web browser. Or just connect to the above address using WebDAV client.");

                    while (Listening)
                    {
                        HttpListenerContext context = await listener.GetContextAsync();
#pragma warning disable 4014
                        Task.Factory.StartNew(() => ProcessRequestAsync(listener, context));
#pragma warning restore 4014
                    }
                }
            }
            catch(Exception ex)
            {
                configuration.DavLoggerOptions.LogOutput(ex.Message);
                configuration.DavLoggerOptions.LogOutput(ex.StackTrace);
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

                // Uncomment code below to allow digest authentication mechanism.
                /*ListenerAuthentication listenerAuthentication = new ListenerAuthentication(digestProvider);
                principal = listenerAuthentication.PerformAuthentication(context);
                if(context.Response.StatusCode == 401)
                {
                    return;
                }*/

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

        /// <summary>
        /// Checks user name and returns his password and roles.
        /// </summary>
        /// <param name="username">Current user name.</param>
        /// <returns>Passwords and roles for specified user if he exists.</returns>
        private DigestAuthenticationProvider.PasswordAndRoles GetPasswordAndRoles(string username)
        {
            foreach(DavUser user in configuration.DavUsers.Users)
            {
                if(user.Name == username)
                {
                    return new DigestAuthenticationProvider.PasswordAndRoles(user.Password, new[] { "admin" });
                }
            }
            return null;
        }

        /// <summary>
        /// Returns current network interface IPv4 address.
        /// </summary>
        /// <returns>Current network interface IPv4 address</returns>
        private IPAddress LocalIPAddress()
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
            {
                return null;
            }

            foreach (var netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                foreach (var addrInfo in netInterface.GetIPProperties().UnicastAddresses)
                {
                    if (addrInfo.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(addrInfo.Address))
                    {
                        return addrInfo.Address;
                    }
                }
            }

            return null;
        }
    }
}
