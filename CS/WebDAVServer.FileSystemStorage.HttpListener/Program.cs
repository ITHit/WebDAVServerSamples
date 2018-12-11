using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Xml;
using System.Threading.Tasks;
using ITHit.WebDAV.Server.Logger;
using ITHit.WebDAV.Server;
using WebDAVServer.FileSystemStorage.HttpListener;
using System.Net.WebSockets;
using System.Threading;
using WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes;

namespace WebDAVServer.FileSystemStorage.HttpListener
{
    /// <summary>
    /// WebDAV engine host.
    /// </summary>
    internal class Program
    {
        public static bool Listening { get; set; }

        private static DavEngineAsync engine;

        private static readonly string repositoryPath =
            ConfigurationManager.AppSettings["RepositoryPath"].TrimEnd(Path.DirectorySeparatorChar);

        /// <summary>
        /// Whether requests/responses shall be logged.
        /// </summary>
        private static readonly bool debugLoggingEnabled =
            ConfigurationManager.AppSettings["DebugLoggingEnabled"].Equals(
                "true",
                StringComparison.InvariantCultureIgnoreCase);

        /// <summary>
        /// Logger instance.
        /// </summary>
        private static readonly DefaultLoggerImpl logger = new DefaultLoggerImpl();

        /// <summary>
        /// Gets a value indicating whether the program is runing as a Windows service or standalone application.
        /// </summary>
        private static bool IsServiceMode
        {
            get { return !Environment.UserInteractive; }
        }

        /// <summary>
        /// Entry point.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static void Main(string[] args)
        {
            try
            {
                CheckConfigErrors();
                Init();
                if (IsServiceMode)
                {
                    Listening = false;
                    ServiceBase.Run(new Service());
                }
                else
                {
                    Listening = true;
                    ThreadProcAsync();
                    Console.ReadKey();
                }
            }
            catch (Exception ex)
            {
                if (IsServiceMode)
                {
                    string errorMessage = "Could not start service" + Environment.NewLine + " " + ex.Message;
                    logger.LogError(errorMessage, null);
                }
                else
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine(Environment.NewLine + "Press any key...");
                    Console.ReadKey();
                }
            }
        }

        static void Init()
        {
            string contentRootPath = Directory.GetParent(Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath)).FullName;
            string logPath = Path.Combine(contentRootPath, @"App_Data\WebDav\Logs");
            logger.LogFile = Path.Combine(logPath, "WebDAVlog.txt");
            logger.IsDebugEnabled = debugLoggingEnabled;

            engine = new DavEngineAsync
            {
                Logger = logger

                // Use idented responses if debug logging is enabled.
                , OutputXmlFormatting = true
            };

            /// This license lile is used to activate:
            ///  - IT Hit WebDAV Server Engine for .NET
            ///  - IT Hit iCalendar and vCard Library if used in a project
            string license = File.ReadAllText(Path.Combine(contentRootPath, "License.lic"));

            engine.License = license;

            // Set custom handler to process GET and HEAD requests to folders and display 
            // info about how to connect to server. We are using the same custom handler 
            // class (but different instances) here to process both GET and HEAD because 
            // these requests are very similar. Some WebDAV clients may fail to connect if HEAD 
            // request is not processed.
            MyCustomGetHandler handlerGet  = new MyCustomGetHandler(contentRootPath);
            MyCustomGetHandler handlerHead = new MyCustomGetHandler(contentRootPath);
            handlerGet.OriginalHandler  = engine.RegisterMethodHandler("GET",  handlerGet);
            handlerHead.OriginalHandler = engine.RegisterMethodHandler("HEAD", handlerHead);
            string attrStoragePath = (ConfigurationManager.AppSettings["AttrStoragePath"] ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar);

            if (!string.IsNullOrEmpty(attrStoragePath))
            {
                FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(attrStoragePath, repositoryPath));
            }
            else if (!(new DirectoryInfo(repositoryPath).IsExtendedAttributesSupported()))
            {
                var tempPath = Path.Combine(Path.GetTempPath(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
                FileSystemInfoExtension.UseFileSystemAttribute(new FileSystemExtendedAttribute(tempPath, repositoryPath));
            }
        }

        public static async void ThreadProcAsync()
        {
            string uriPrefix = ConfigurationManager.AppSettings["ListenerPrefix"];
            using (System.Net.HttpListener listener = new System.Net.HttpListener())
            {
                listener.Prefixes.Add(uriPrefix);

                listener.IgnoreWriteExceptions = true;

                // For the sake of the development convenience, this code opens default web browser
                // with this server url when project is started in the debug mode as a console app.
#if DEBUG
                if (!IsServiceMode)
                {
                    System.Diagnostics.Process.Start(uriPrefix.Replace("+", "localhost"), null);
                }
#endif


                listener.Start();

                Console.WriteLine("Start listening on " + uriPrefix);
                Console.WriteLine("Press Control-C to stop listener...");

                while (Listening)
                {
                    HttpListenerContext context = await listener.GetContextAsync();
#pragma warning disable 4014
                    Task.Factory.StartNew(() => ProcessRequestAsync(listener, context));
#pragma warning restore 4014
                }
            }
        }

        private static async Task ProcessWebSocketRequestAsync(HttpListenerContext context)
        {
            WebSocketsService socketService = WebSocketsService.Service;
            WebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket client = webSocketContext.WebSocket;

            // Adding client to connected clients collection.
            Guid clientId = socketService.AddClient(client);

            byte[] buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = null;

            while (client.State == WebSocketState.Open)
            {
                try
                {
                    // Must receive client results.
                    result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                }
                catch (WebSocketException)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
            }

            // Remove client from connected clients dictionary after disconnecting.
            socketService.RemoveClient(clientId);
        }

        private static async Task ProcessRequestAsync(System.Net.HttpListener listener, HttpListenerContext context)
        {
            try
            { 
                MacOsXPreprocessor.Process(context.Request); // fixes headers for Mac OS X v10.5.3 or later

                IPrincipal principal = null;
                if (context.Request.IsWebSocketRequest)
                {
                    // If current request is web socket request.
                    await ProcessWebSocketRequestAsync(context);
                    return;
                }

                context.Response.SendChunked = false;

                var ntfsDavContext =
                    new DavContext(context, listener.Prefixes, principal, repositoryPath, engine.Logger);

                if ((principal != null) && principal.Identity.IsAuthenticated)
                {
                }
                await engine.RunAsync(ntfsDavContext);

                if (context.Response.StatusCode == 401)
                {
                    ShowLoginDialog(context, context.Response);
                }
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
        private static void ShowLoginDialog(HttpListenerContext context, HttpListenerResponse response)
        {

            if (!context.Request.HttpMethod.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase)
                && !context.Request.HttpMethod.Equals("HEAD", StringComparison.InvariantCultureIgnoreCase))
            {
                byte[] message = new UTF8Encoding().GetBytes("Access is denied.");
                context.Response.ContentLength64 = message.Length;
                context.Response.OutputStream.Write(message, 0, message.Length);
            }
        }

        /// <summary>
        /// Checks configuration errors.
        /// </summary>
        private static void CheckConfigErrors()
        {
            string repPath = ConfigurationManager.AppSettings["RepositoryPath"];
            if (repPath == null || !Directory.Exists(repPath))
            {
                throw new Exception("Invalid RepositoryPath configuration parameter value.");
            }

            string uriPrefix = ConfigurationManager.AppSettings["ListenerPrefix"];
            if (string.IsNullOrEmpty(uriPrefix))
            {
                throw new Exception("ListenerPrefix section is missing or invalid!");
            }
        }
    }
}
