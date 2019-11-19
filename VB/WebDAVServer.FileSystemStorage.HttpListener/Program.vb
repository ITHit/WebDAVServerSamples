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
Imports WebDAVServer.FileSystemStorage.HttpListener.ExtendedAttributes
Imports ITHit.GSuite.Server

''' <summary>
''' WebDAV engine host.
''' </summary>
Friend Class Program

    Public Shared Property Listening As Boolean

    Private Shared engine As DavEngineAsync

    Private Shared gSuiteEngine As GSuiteEngineAsync

    Private Shared ReadOnly googleServiceAccountID As String = ConfigurationManager.AppSettings("GoogleServiceAccountID")

    Private Shared ReadOnly googleServicePrivateKey As String = ConfigurationManager.AppSettings("GoogleServicePrivateKey")

    Private ReadOnly gSuiteLicense As String = File.ReadAllText(HttpContext.Current.Request.PhysicalApplicationPath & "GSuiteLicense.lic")

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
        engine = New DavEngineAsync With {.Logger = logger,
                                    .OutputXmlFormatting = True}
        ''' This license lile is used to activate:
        '''  - IT Hit WebDAV Server Engine for .NET
        '''  - IT Hit iCalendar and vCard Library if used in a project
        Dim license As String = File.ReadAllText(Path.Combine(contentRootPath, "License.lic"))
        engine.License = license
        gSuiteEngine = New GSuiteEngineAsync(googleServiceAccountID, googleServicePrivateKey) With {.License = gSuiteLicense,
                                                                                              .Logger = logger
                                                                                              }
        ' Set custom handler to process GET and HEAD requests to folders and display 
        ' info about how to connect to server. We are using the same custom handler 
        ' class (but different instances) here to process both GET and HEAD because 
        ' these requests are very similar. Some WebDAV clients may fail to connect if HEAD 
        ' request is not processed.
        Dim handlerGet As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        Dim handlerHead As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        handlerGet.OriginalHandler = engine.RegisterMethodHandler("GET", handlerGet)
        handlerHead.OriginalHandler = engine.RegisterMethodHandler("HEAD", handlerHead)
        Dim attrStoragePath As String =(If(ConfigurationManager.AppSettings("AttrStoragePath"), String.Empty)).TrimEnd(Path.DirectorySeparatorChar)
        If Not String.IsNullOrEmpty(attrStoragePath) Then
            FileSystemInfoExtension.UseFileSystemAttribute(New FileSystemExtendedAttribute(attrStoragePath, repositoryPath))
        ElseIf Not(New DirectoryInfo(repositoryPath).IsExtendedAttributesSupported()) Then
            Dim tempPath = Path.Combine(Path.GetTempPath(), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name)
            FileSystemInfoExtension.UseFileSystemAttribute(New FileSystemExtendedAttribute(tempPath, repositoryPath))
        End If
    End Sub

    Public Shared Async Sub ThreadProcAsync()
        Dim uriPrefix As String = ConfigurationManager.AppSettings("ListenerPrefix")
        Using listener As System.Net.HttpListener = New System.Net.HttpListener()
            listener.Prefixes.Add(uriPrefix)
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
            If context.Request.IsWebSocketRequest Then
                ' If current request is web socket request.
                Await ProcessWebSocketRequestAsync(context)
                Return
            End If

            context.Response.SendChunked = False
            Dim ntfsDavContext = New DavContext(context, listener.Prefixes, principal, repositoryPath, engine.Logger)
            If(principal IsNot Nothing) AndAlso principal.Identity.IsAuthenticated Then
                 End If

            Await engine.RunAsync(ntfsDavContext)
            Await gSuiteEngine.RunAsync(ContextConverter.ConvertToGSuiteContext(ntfsDavContext))
            If context.Response.StatusCode = 401 Then
                ShowLoginDialog(context, context.Response)
            End If
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

        Dim googleServiceAccountID As String = ConfigurationManager.AppSettings("GoogleServiceAccountID")
        If String.IsNullOrEmpty(googleServiceAccountID) Then
            Throw New Exception("GoogleServiceAccountID is not specified.")
        End If

        Dim googleServicePrivateKey As String = ConfigurationManager.AppSettings("GoogleServicePrivateKey")
        If String.IsNullOrEmpty(googleServicePrivateKey) Then
            Throw New Exception("GoogleServicePrivateKey is not specified.")
        End If
    End Sub
End Class
