Imports System
Imports System.Configuration
Imports System.IO
Imports System.Data.SqlClient
Imports System.Linq
Imports System.Net
Imports System.Reflection
Imports System.Security.Principal
Imports System.ServiceProcess
Imports System.Text
Imports System.Xml
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server.Logger
Imports ITHit.WebDAV.Server
Imports WebDAVServer.SqlStorage.HttpListener
Imports System.Net.WebSockets
Imports System.Threading
Imports ITHit.Server
Imports ITHit.GSuite.Server

''' <summary>
''' WebDAV engine host.
''' </summary>
Friend Class Program

    Public Shared Property Listening As Boolean

    Private Shared webDavEngine As DavEngineAsync

    Private Shared gSuiteEngine As GSuiteEngineAsync

    ''' <summary>
    ''' Google Service Account ID (client_email field from JSON file).
    ''' </summary>
    Private Shared ReadOnly googleServiceAccountID As String = ConfigurationManager.AppSettings("GoogleServiceAccountID")

    ''' <summary>
    ''' Google Service private key (private_key field from JSON file).
    ''' </summary>
    Private Shared ReadOnly googleServicePrivateKey As String = ConfigurationManager.AppSettings("GoogleServicePrivateKey")

    ''' <summary>
    ''' Relative Url of "Webhook" callback. It handles the API notification messages that are triggered when a resource changes.
    ''' </summary>
    Private Shared ReadOnly googleNotificationsRelativeUrl As String = ConfigurationManager.AppSettings("GoogleNotificationsRelativeUrl")

    ''' <summary>
    ''' Whether requests/responses shall be logged.
    ''' </summary>
    Private Shared ReadOnly debugLoggingEnabled As Boolean = ConfigurationManager.AppSettings("DebugLoggingEnabled").Equals("true",
                                                                                                                           StringComparison.InvariantCultureIgnoreCase)

    ''' <summary>
    ''' Logger instance.
    ''' </summary>
    Private Shared ReadOnly logger As DefaultLoggerImpl = New DefaultLoggerImpl()

    ''' <summary>
    ''' Gets a value indicating whether the program is runing as a Windows service or standalone application.
    ''' </summary>
    Private Shared ReadOnly Property IsServiceMode As Boolean
        Get
            Return Not Environment.UserInteractive
        End Get
    End Property

    ''' <summary>
    ''' Entry point.
    ''' </summary>
    ''' <param name="args">Command line arguments.</param>
    Public Shared Sub Main(args As String())
        Try
            CheckConfigErrors()
            Init()
            If IsServiceMode Then
                Listening = False
                ServiceBase.Run(New Service())
            Else
                Listening = True
                ThreadProcAsync()
                Console.ReadKey()
            End If
        Catch ex As Exception
            If IsServiceMode Then
                Dim errorMessage As String = "Could not start service" & Environment.NewLine & " " & ex.Message
                logger.LogError(errorMessage, Nothing)
            Else
                Console.WriteLine(ex.Message)
                Console.WriteLine(Environment.NewLine & "Press any key...")
                Console.ReadKey()
            End If

        End Try
    End Sub

    Private Shared Sub Init()
        Dim contentRootPath As String = Directory.GetParent(Path.GetDirectoryName(New Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath)).FullName
        Dim logPath As String = Path.Combine(contentRootPath, "App_Data\WebDav\Logs")
        logger.LogFile = Path.Combine(logPath, "WebDAVlog.txt")
        logger.IsDebugEnabled = debugLoggingEnabled
        webDavEngine = New DavEngineAsync With {.Logger = logger,
                                          .OutputXmlFormatting = True}
        ''' This license lile is used to activate:
        '''  - IT Hit WebDAV Server Engine for .NET
        '''  - IT Hit iCalendar and vCard Library if used in a project
        Dim license As String = File.ReadAllText(Path.Combine(contentRootPath, "License.lic"))
        webDavEngine.License = license
        ''' This license file is used to activate G Suite Documents Editing for IT Hit WebDAV Server
        Dim gSuiteLicense As String = If(File.Exists(Path.Combine(contentRootPath, "GSuiteLicense.lic")), File.ReadAllText(Path.Combine(contentRootPath, "GSuiteLicense.lic")), String.Empty)
        If Not String.IsNullOrEmpty(gSuiteLicense) Then
            gSuiteEngine = New GSuiteEngineAsync(googleServiceAccountID, googleServicePrivateKey, googleNotificationsRelativeUrl) With {.License = gSuiteLicense,
                                                                                                                                  .Logger = logger
                                                                                                                                  }
        End If

        ' Set custom handler to process GET and HEAD requests to folders and display 
        ' info about how to connect to server. We are using the same custom handler 
        ' class (but different instances) here to process both GET and HEAD because 
        ' these requests are very similar. Some WebDAV clients may fail to connect if HEAD 
        ' request is not processed.
        Dim handlerGet As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        Dim handlerHead As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        handlerGet.OriginalHandler = webDavEngine.RegisterMethodHandler("GET", handlerGet)
        handlerHead.OriginalHandler = webDavEngine.RegisterMethodHandler("HEAD", handlerHead)
    End Sub

    Public Shared Async Sub ThreadProcAsync()
        Dim uriPrefix As String = ConfigurationManager.AppSettings("ListenerPrefix")
        Using listener As System.Net.HttpListener = New System.Net.HttpListener()
            listener.Prefixes.Add(uriPrefix)
            listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous
            listener.IgnoreWriteExceptions = True
            ' For the sake of the development convenience, this code opens default web browser
            ' with this server url when project is started in the debug mode as a console app.
            If Not IsServiceMode Then
                System.Diagnostics.Process.Start(uriPrefix.Replace("+", "localhost"), Nothing)
            End If

            listener.Start()
            Console.WriteLine("Start listening on " & uriPrefix)
            Console.WriteLine("Press Control-C to stop listener...")
            While Listening
                Dim context As HttpListenerContext = Await listener.GetContextAsync()
                Task.Factory.StartNew(Function() ProcessRequestAsync(listener, context))
            End While
        End Using
    End Sub

    Private Shared Async Function ProcessWebSocketRequestAsync(context As HttpListenerContext) As Task
        Dim socketService As WebSocketsService = WebSocketsService.Service
        Dim webSocketContext As WebSocketContext = Await context.AcceptWebSocketAsync(Nothing)
        Dim client As WebSocket = webSocketContext.WebSocket
        ' Adding client to connected clients collection.
        Dim clientId As Guid = socketService.AddClient(client)
        Dim buffer As Byte() = New Byte(4095) {}
        Dim result As WebSocketReceiveResult = Nothing
        While client.State = WebSocketState.Open
            Try
                ' Must receive client results.
                result = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None)
            Catch __unusedWebSocketException1__ As WebSocketException
                Exit While
            End Try

            If result.MessageType = WebSocketMessageType.Close Then
                Await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None)
            End If
        End While

        ' Remove client from connected clients dictionary after disconnecting.
        socketService.RemoveClient(clientId)
    End Function

    Private Shared Async Function ProcessRequestAsync(listener As System.Net.HttpListener, context As HttpListenerContext) As Task
        Try
            MacOsXPreprocessor.Process(context.Request)
            Dim principal As IPrincipal = Nothing
            If context.Request.IsWebSocketRequest AndAlso context.Request.RawUrl.StartsWith("/dav") Then
                ' If current request is web socket request.
                Await ProcessWebSocketRequestAsync(context)
                Return
            End If

            context.Response.SendChunked = False
            Using sqlDavContext = New DavContext(context, listener.Prefixes, principal, webDavEngine.Logger)
                Await webDavEngine.RunAsync(sqlDavContext)
                If gSuiteEngine IsNot Nothing Then
                    Await gSuiteEngine.RunAsync(ContextConverter.ConvertToGSuiteContext(sqlDavContext))
                End If
            End Using
        Finally
            If context IsNot Nothing AndAlso context.Response IsNot Nothing Then
                Try
                    context.Response.Close()
                Catch
                    ' client closed connection before the content was sent
                     End Try
            End If
        End Try
    End Function

    ''' <summary>
    ''' Checks configuration errors.
    ''' </summary>
    Private Shared Sub CheckConfigErrors()
        Dim connStr As String = ConfigurationManager.ConnectionStrings("WebDAV").ConnectionString
        If String.IsNullOrEmpty(connStr) Then
            Throw New Exception("SqlConnectionString section is missing or invalid!")
        End If

        Dim exePath As String = Path.GetDirectoryName(New Uri(Assembly.GetExecutingAssembly().EscapedCodeBase).LocalPath)
        Dim dataPath As String = Path.Combine(Directory.GetParent(exePath).FullName, "App_Data")
        connStr = connStr.Replace("|DataDirectory|", dataPath)
        Using conn As SqlConnection = New SqlConnection(connStr)
            conn.Open()
        End Using

        Dim uriPrefix As String = ConfigurationManager.AppSettings("ListenerPrefix")
        If String.IsNullOrEmpty(uriPrefix) Then
            Throw New Exception("ListenerPrefix section is missing or invalid!")
        End If
    End Sub
End Class
