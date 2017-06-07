Imports System
Imports System.Configuration
Imports System.IO
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
Imports WebDAVServer.FileSystemStorage.HttpListener
Imports System.Net.WebSockets
Imports System.Threading

''' <summary>
''' WebDAV engine host.
''' </summary>
Friend Class Program

    Public Shared Property Listening As Boolean

    Private Shared engine As DavEngineAsync

    Private Shared ReadOnly repositoryPath As String = ConfigurationManager.AppSettings("RepositoryPath").TrimEnd(Path.DirectorySeparatorChar)

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
        engine = New DavEngineAsync With {.Logger = logger, .OutputXmlFormatting = True}
        Dim license As String = File.ReadAllText(Path.Combine(contentRootPath, "License.lic"))
        engine.License = license
        Dim handlerGet As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        Dim handlerHead As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        handlerGet.OriginalHandler = engine.RegisterMethodHandler("GET", handlerGet)
        handlerHead.OriginalHandler = engine.RegisterMethodHandler("HEAD", handlerHead)
    End Sub

    Public Shared Async Sub ThreadProcAsync()
        Dim uriPrefix As String = ConfigurationManager.AppSettings("ListenerPrefix")
        Using listener As System.Net.HttpListener = New System.Net.HttpListener()
            listener.Prefixes.Add(uriPrefix)
            listener.IgnoreWriteExceptions = True
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
        Dim clientId As Guid = socketService.AddClient(client)
        Dim buffer As Byte() = New Byte(4095) {}
        Dim result As WebSocketReceiveResult = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None)
        While Not result.CloseStatus.HasValue
            ' Must receive client results to update client state and detect disconnecting.
            result = Await client.ReceiveAsync(New ArraySegment(Of Byte)(buffer), CancellationToken.None)
        End While

        ' Remove client from connected clients collection after disconnecting.
        socketService.RemoveClient(clientId)
        Await client.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None)
    End Function

    Private Shared Async Function ProcessRequestAsync(listener As System.Net.HttpListener, context As HttpListenerContext) As Task
        Try
            MacOsXPreprocessor.Process(context.Request)
            Dim principal As IPrincipal = Nothing
            If context.Request.IsWebSocketRequest Then
                Await ProcessWebSocketRequestAsync(context)
                Return
            End If

            context.Response.SendChunked = False
            Dim ntfsDavContext = New DavContext(context, listener.Prefixes, principal, repositoryPath, engine.Logger)
            If(principal IsNot Nothing) AndAlso principal.Identity.IsAuthenticated Then
            End If

            Await engine.RunAsync(ntfsDavContext)
            If context.Response.StatusCode = 401 Then
                ShowLoginDialog(context, context.Response)
            End If
        Finally
            If context IsNot Nothing AndAlso context.Response IsNot Nothing Then
                Try
                    context.Response.Close()
                Catch
                End Try
            End If
        End Try
    End Function

    Private Shared Sub ShowLoginDialog(context As HttpListenerContext, response As HttpListenerResponse)
        If Not context.Request.HttpMethod.Equals("OPTIONS", StringComparison.InvariantCultureIgnoreCase) AndAlso Not context.Request.HttpMethod.Equals("HEAD", StringComparison.InvariantCultureIgnoreCase) Then
            Dim message As Byte() = New UTF8Encoding().GetBytes("Access is denied.")
            context.Response.ContentLength64 = message.Length
            context.Response.OutputStream.Write(message, 0, message.Length)
        End If
    End Sub

    ''' <summary>
    ''' Checks configuration errors.
    ''' </summary>
    Private Shared Sub CheckConfigErrors()
        Dim repPath As String = ConfigurationManager.AppSettings("RepositoryPath")
        If repPath Is Nothing OrElse Not Directory.Exists(repPath) Then
            Throw New Exception("Invalid RepositoryPath configuration parameter value.")
        End If

        Dim uriPrefix As String = ConfigurationManager.AppSettings("ListenerPrefix")
        If String.IsNullOrEmpty(uriPrefix) Then
            Throw New Exception("ListenerPrefix section is missing or invalid!")
        End If
    End Sub
End Class
