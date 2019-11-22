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
using ITHit.GSuite.Server;

namespace WebDAVServer.SqlStorage.HttpListener
{
    /// <summary>
    /// WebDAV engine host.
    /// </summary>
    internal class Program
    {
        public static bool Listening { get; set; }

        private static DavEngineAsync webDavEngine;

        private static GSuiteEngineAsync gSuiteEngine;

        /// <summary>
        /// Google Service Account ID (client_email field from JSON file).
        /// </summary>
        private static readonly string googleServiceAccountID = ConfigurationManager.AppSettings["GoogleServiceAccountID"];

        /// <summary>
        /// Google Service private key (private_key field from JSON file).
        /// </summary>
        private static readonly string googleServicePrivateKey = ConfigurationManager.AppSettings["GoogleServicePrivateKey"];

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

            webDavEngine = new DavEngineAsync
            {
                Logger = logger

                // Use idented responses if debug logging is enabled.
                , OutputXmlFormatting = true
            };

            /// This license lile is used to activate:
            ///  - IT Hit WebDAV Server Engine for .NET
            ///  - IT Hit iCalendar and vCard Library if used in a project
            string license = File.ReadAllText(Path.Combine(contentRootPath, "License.lic"));

            webDavEngine.License = license;

            /// This license file is used to activate G Suite Documents Editing for IT Hit WebDAV Server
            string gSuiteLicense = File.ReadAllText(Path.Combine(contentRootPath,"GSuiteLicense.lic"));
            gSuiteEngine = new GSuiteEngineAsync(googleServiceAccountID, googleServicePrivateKey)
            {
                License = gSuiteLicense,
                Logger = logger
            };

            // Set custom handler to process GET and HEAD requests to folders and display 
            // info about how to connect to server. We are using the same custom handler 
            // class (but different instances) here to process both GET and HEAD because 
            // these requests are very similar. Some WebDAV clients may fail to connect if HEAD 
            // request is not processed.
            MyCustomGetHandler handlerGet  = new MyCustomGetHandler(contentRootPath);
            MyCustomGetHandler handlerHead = new MyCustomGetHandler(contentRootPath);
            handlerGet.OriginalHandler  = webDavEngine.RegisterMethodHandler("GET",  handlerGet);
            handlerHead.OriginalHandler = webDavEngine.RegisterMethodHandler("HEAD", handlerHead);
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

                using (var sqlDavContext = new DavContext(context, listener.Prefixes, principal, webDavEngine.Logger))
                {
                    await webDavEngine.RunAsync(sqlDavContext);
                    await gSuiteEngine.RunAsync(ContextConverter.ConvertToGSuiteContext(sqlDavContext));
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

            string googleServiceAccountID = ConfigurationManager.AppSettings["GoogleServiceAccountID"];
            if (string.IsNullOrEmpty(googleServiceAccountID))
            {
                throw new Exception("GoogleServiceAccountID is not specified.");
            }

            string googleServicePrivateKey = ConfigurationManager.AppSettings["GoogleServicePrivateKey"];
            if (string.IsNullOrEmpty(googleServicePrivateKey))
            {
                throw new Exception("GoogleServicePrivateKey is not specified.");
            }
        }
    }
}
