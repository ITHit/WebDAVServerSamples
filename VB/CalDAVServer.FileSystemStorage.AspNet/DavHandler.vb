Imports System
Imports System.Collections.Generic
Imports System.Configuration
Imports System.IO
Imports System.Security.Principal
Imports System.Web
Imports System.Xml
Imports System.Xml.Serialization
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports CalDAVServer.FileSystemStorage.AspNet
Imports CalDAVServer.FileSystemStorage.AspNet.Acl

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

    Public Overrides Async Function ProcessRequestAsync(context As HttpContext) As Task
        Dim engine As DavEngineAsync = getOrInitializeEngine(context)
        context.Response.BufferOutput = False
        Dim ntfsDavContext As DavContext = New DavContext(context)
        Await engine.RunAsync(ntfsDavContext)
    End Function

    Private Function initializeEngine(context As HttpContext) As DavEngineAsync
        Dim logger As ILogger = CalDAVServer.FileSystemStorage.AspNet.Logger.Instance
        logger.LogFlags = LogFlagsEnum.LogGetResponseBody Or LogFlagsEnum.LogPutRequestBody
        Dim engine As DavEngineAsync = New DavEngineAsync With {.Logger = logger, .OutputXmlFormatting = True, .CorsAllowedFor = Nothing, .UseFullUris = False}
        engine.License = license
        Dim contentRootPath As String = HttpContext.Current.Request.MapPath("/")
        Dim handlerGet As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        Dim handlerHead As MyCustomGetHandler = New MyCustomGetHandler(contentRootPath)
        handlerGet.OriginalHandler = engine.RegisterMethodHandler("GET", handlerGet)
        handlerHead.OriginalHandler = engine.RegisterMethodHandler("HEAD", handlerHead)
        ' Set your iCalendar & vCard library license before calling any members.
        ' iCalendar & vCard library accepts:
        ' - WebDAV Server Engine license with iCalendar & vCard modules. Verify your license file to see if these modules are specified.
        ' - or iCalendar and vCard Library license.
        ITHit.Collab.LicenseValidator.SetLicense(license)
        Return engine
    End Function

    Private Function getOrInitializeEngine(context As HttpContext) As DavEngineAsync
        Const ENGINE_KEY As String = "$DavEngine$"
        If context.Application(ENGINE_KEY) Is Nothing Then
            context.Application(ENGINE_KEY) = initializeEngine(context)
        End If

        Return CType(context.Application(ENGINE_KEY), DavEngineAsync)
    End Function
End Class
