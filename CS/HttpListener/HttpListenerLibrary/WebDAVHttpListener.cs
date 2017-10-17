using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

using HttpListenerLibrary.Options;
using ITHit.WebDAV.Server;

namespace HttpListenerLibrary
{
    /// <summary>
    /// HttpListener implementation for WebDavServer.
    /// </summary>
    public class WebDAVHttpListener : IServer
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
        /// Server features collection.
        /// </summary>
        public IFeatureCollection Features { get; } = new FeatureCollection();

        /// <summary>
        /// IT Hit WebDav engine instance.
        /// </summary>
        private DavEngineAsync engine;

        /// <summary>
        /// Logger instance.
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// Represents log method to output messages to view.
        /// </summary>
        private ILogMethod logMethod;

        /// <summary>
        /// Notification service instance.
        /// </summary>
        private EventsService eventsService;

        /// <summary>
        /// Represents context options.
        /// </summary>
        private DavContextOptions contextOptions;

        /// <summary>
        /// Represents users collection.
        /// </summary>
        private DavUserOptions userOptions;

        /// <summary>
        /// Creates instance of this class.
        /// </summary>
        /// <param name="contextOptions">Context options.</param>
        /// <param name="userOptions">User options.</param>
        /// <param name="logger">Logger instance.</param>
        /// <param name="logMethod">Logging method for specific platform.</param>
        /// <param name="eventsService">Server sent events service for user notifications.</param>
        /// <param name="engine">Engine instance.</param>
        public WebDAVHttpListener(
            IOptions<DavContextOptions> contextOptions,
            IOptions<DavUserOptions> userOptions,
            ILogger logger,
            ILogMethod logMethod,
            EventsService eventsService,
            DavEngineAsync engine
            )
        {
            this.contextOptions = contextOptions.Value;
            this.userOptions = userOptions.Value;
            this.logger = logger;
            this.eventsService = eventsService;
            this.engine = engine;
            this.logMethod = logMethod;
            Features.Set<IHttpRequestFeature>(new HttpRequestFeature());
            Features.Set<IHttpResponseFeature>(new HttpResponseFeature());
            digestProvider = new DigestAuthenticationProvider(GetPasswordAndRoles);
        }

        /// <summary>
        /// Starts HttpListener.
        /// </summary>
        public async Task StartAsync<TContext>(IHttpApplication<TContext> application, CancellationToken cancellationToken)
        {
            try
            {
                Listening = true;
                ThreadProcAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message, ex);
            }
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
                    listener.Prefixes.Add(contextOptions.ListenerPrefix);

                    // We do not use AuthenticationSchemes.Digest here because OPTIONS request must be processed without authentication. 
                    // Instead this sample provides its own Digest authentication implementation.
                    listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

                    listener.IgnoreWriteExceptions = true;

                    listener.Start();

                    string listenerPrefix = contextOptions.ListenerPrefix.Replace("+", LocalIPAddress().ToString());
                    string startMessage = $"Started listening on {contextOptions.ListenerPrefix}.\n\n" +
                        $"To access your files go to {listenerPrefix} in a web browser. Or just connect to the above address using WebDAV client.";
                    logger.LogDebug(startMessage);
                    logMethod.LogOutput(startMessage);

                    while (Listening)
                    {
                        HttpListenerContext context = await listener.GetContextAsync();
#pragma warning disable 4014
                        Task.Factory.StartNew(() => ProcessRequestAsync(listener, context));
#pragma warning restore 4014
                    }
                }
            }
            catch(HttpListenerException ex) when (ex.ErrorCode == 5)
            {
                logger.LogError("Access is denied, try to run with administrative privileges.", null);
            }
            catch(Exception ex)
            {
                logger.LogError(ex.Message, ex);
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

                // Uncomment the code below to enable Digest authentication.
                /*ListenerAuthentication listenerAuthentication = new ListenerAuthentication(digestProvider);
                principal = listenerAuthentication.PerformAuthentication(context);
                if(context.Response.StatusCode == 401)
                {
                    return;
                }*/

                if (context.Request.Headers["Accept"] == "text/event-stream")
                {
                    context.Response.ContentType = "text/event-stream";
                    context.Response.OutputStream.Flush();
                    Guid clientId = eventsService.AddClient(context.Response);
                    return;
                }

                context.Response.SendChunked = false;
                context.Response.KeepAlive = false;

                var ntfsDavContext =
                    new DavContext(context, listener.Prefixes, principal, contextOptions.RepositoryPath, engine.Logger, eventsService);

                await engine.RunAsync(ntfsDavContext);
            }
            finally
            {
                if (context != null && context.Response != null && context.Response.ContentType != "text/event-stream")
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
            foreach(DavUser user in userOptions.Users)
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

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("192.168.0.1", 65535);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {

        }

        public void Dispose()
        {
            
        }
    }

    /// <summary>
    /// Extension methods for using HttpListener to listen to incoming requests.
    /// </summary>
    public static class WebHostBuilderExtensions
    {
        /// <summary>
        /// Includes HttpListener server logic.
        /// </summary>
        /// <param name="webHostBuilder">Instance of <see cref="IWebHostBuilder"/>.</param>
        /// <returns>Instance of <see cref="IWebHostBuilder"/>.</returns>
        public static IWebHostBuilder UseHttpListener(this IWebHostBuilder webHostBuilder)
        {
            return webHostBuilder.ConfigureServices(services => services.AddSingleton<IServer, WebDAVHttpListener>());
        }
    }
}
