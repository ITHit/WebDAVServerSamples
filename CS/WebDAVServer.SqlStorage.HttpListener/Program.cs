using System;
using System.Configuration;
using System.IO;
using System.Data.SqlClient;
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
using WebDAVServer.SqlStorage.HttpListener;
using System.Net.WebSockets;
using System.Threading;

namespace WebDAVServer.SqlStorage.HttpListener
{
    /// <summary>
    /// WebDAV engine host.
    /// </summary>
    internal class Program
    {
        public static bool Listening { get; set; }

        private static DavEngineAsync engine;

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
        }

        public static async void ThreadProcAsync()
        {
            string uriPrefix = ConfigurationManager.AppSettings["ListenerPrefix"];
            using (System.Net.HttpListener listener = new System.Net.HttpListener())
            {
                listener.Prefixes.Add(uriPrefix);

                listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;

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
            WebSocketReceiveResult result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!result.CloseStatus.HasValue)
            {
                // Must receive client results to update client state and detect disconnecting.
                result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }

            // Remove client from connected clients collection after disconnecting.
            socketService.RemoveClient(clientId);

            await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
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

                using (var sqlDavContext = new DavContext(context, listener.Prefixes, principal, engine.Logger))
                {
                    await engine.RunAsync(sqlDavContext);
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

        /// <summary>
        /// Checks configuration errors.
        /// </summary>
        private static void CheckConfigErrors()
        {
            string connStr = ConfigurationManager.ConnectionStrings["WebDAV"].ConnectionString;
            if (string.IsNullOrEmpty(connStr))
            {
                throw new Exception("SqlConnectionString section is missing or invalid!");
            }

            string exePath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath);
            string dataPath = Path.Combine(Directory.GetParent(exePath).FullName, "App_Data");
            connStr = connStr.Replace("|DataDirectory|", dataPath);
            // test database connection
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
            }

            string uriPrefix = ConfigurationManager.AppSettings["ListenerPrefix"];
            if (string.IsNullOrEmpty(uriPrefix))
            {
                throw new Exception("ListenerPrefix section is missing or invalid!");
            }
        }
    }
}
