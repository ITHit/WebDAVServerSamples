Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.IO
Imports System.Security.Principal
Imports System.Web
Imports System.Xml
Imports System.Xml.Serialization
Imports System.Threading.Tasks
Imports ITHit.Server
Imports ITHit.WebDAV.Server
Imports CardDAVServer.FileSystemStorage.AspNet
Imports CardDAVServer.FileSystemStorage.AspNet.Acl

''' <summary>
''' This handler processes WebDAV requests.
''' </summary>
Public Class DavHandler
    Inherits HttpTaskAsyncHandler

    ''' <summary>
    ''' This license file is used to activate:
    '''  - IT Hit WebDAV Server Engine for .NET
    '''  - IT Hit iCalendar and vCard Library if used in a project
    ''' </summary>
    Private ReadOnly license As String = File.ReadAllText(HttpContext.Current.Request.PhysicalApplicationPath & "License.lic")

    ''' <summary>
    ''' If debug logging is enabled reponses are output as formatted XML,
    ''' all requests and response headers and most bodies are logged.
    ''' If debug logging is disabled only errors are logged.
    ''' </summary>
    Private Shared ReadOnly debugLoggingEnabled As Boolean = "true".Equals(ConfigurationManager.AppSettings("DebugLoggingEnabled"),
                                                                          StringComparison.InvariantCultureIgnoreCase)

    ''' <summary>
    ''' Initializes serialization assemblies.
    ''' </summary>
    Shared Sub New()
        Using ms As MemoryStream = New MemoryStream()
            Call New XmlSerializer(GetType(List(Of PropertyValue))).Serialize(ms, New List(Of PropertyValue)())
            Call New XmlSerializer(GetType(Long)).Serialize(ms, 0L)
            Call New XmlSerializer(GetType(Integer)).Serialize(ms, 0)
        End Using
    End Sub

    ''' <summary>
    ''' Gets a value indicating whether another request can use the
    ''' <see cref="T:System.Web.IHttpHandler"/>  instance.
    ''' </summary>
    ''' <returns>
    ''' true if the <see cref="T:System.Web.IHttpHandler"/>  instance is reusable; otherwise, false.
    ''' </returns>
    Public Overrides ReadOnly Property IsReusable As Boolean
        Get
            Return True
        End Get
    End Property

    ''' <summary>
    ''' Enables processing of HTTP Web requests.
    ''' </summary>
    ''' <param name="context">An <see cref="T:System.Web.HttpContext"/>  object that provides references to the
    ''' intrinsic server objects (for example, Request, Response, Session, and Server) used to service
    ''' HTTP requests. 
    ''' </param>
    Public Overrides Async Function ProcessRequestAsync(context As HttpContext) As Task
        Dim webDavEngine As DavEngineAsync = getOrInitializeWebDavEngine(context)
        context.Response.BufferOutput = False
        Dim ntfsDavContext As DavContext = New DavContext(context)
        Await webDavEngine.RunAsync(ntfsDavContext)
    End Function

    ''' <summary>
    ''' Initializes engine.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="HttpContext"/> .</param>
    ''' <returns>Initialized <see cref="DavEngine"/> .</returns>
    Private Function initializeWebDavEngine(context As HttpContext) As DavEngineAsync
        Dim logger As ILogger = CardDAVServer.FileSystemStorage.AspNet.Logger.Instance
        logger.LogFlags = LogFlagsEnum.LogGetResponseBody Or LogFlagsEnum.LogPutRequestBody
        Dim webDavEngine As DavEngineAsync = New DavEngineAsync With {.Logger = logger,
                                                                .OutputXmlFormatting = True, .CorsAllowedFor = Nothing, .UseFullUris = False}
        webDavEngine.License = license
        Dim contentRootPath As String = HttpContext.Current.Request.MapPath("/")
        ' Set custom handler to process GET and HEAD requests to folders and display 
        ' info about how to connect to server. We are using the same custom handler 
        ' class (but different instances) here to process both GET and HEAD because 
        ' these requests are very similar. Some WebDAV clients may fail to connect if HEAD 
        ' request is not processed.
        Dim handlerGet As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        Dim handlerHead As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        handlerGet.OriginalHandler = webDavEngine.RegisterMethodHandler("GET", handlerGet)
        handlerHead.OriginalHandler = webDavEngine.RegisterMethodHandler("HEAD", handlerHead)
        ' Set your iCalendar & vCard library license before calling any members.
        ' iCalendar & vCard library accepts:
        ' - WebDAV Server Engine license with iCalendar & vCard modules. Verify your license file to see if these modules are specified.
        ' - or iCalendar and vCard Library license.
        ITHit.Collab.LicenseValidator.SetLicense(license)
        Return webDavEngine
    End Function

    ''' <summary>
    ''' Initializes or gets engine singleton.
    ''' </summary>
    ''' <param name="context">Instance of <see cref="HttpContext"/> .</param>
    ''' <returns>Instance of <see cref="DavEngineAsync"/> .</returns>
    Private Function getOrInitializeWebDavEngine(context As HttpContext) As DavEngineAsync
        'we don't use any double check lock pattern here because nothing wrong
        'is going to happen if we created occasionally several engines.
        Const ENGINE_KEY As String = "$DavEngine$"
        If context.Application(ENGINE_KEY) Is Nothing Then
            context.Application(ENGINE_KEY) = initializeWebDavEngine(context)
        End If

        Return CType(context.Application(ENGINE_KEY), DavEngineAsync)
    End Function
End Class
