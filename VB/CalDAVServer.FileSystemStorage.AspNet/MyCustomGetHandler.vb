Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Linq
Imports System.Text
Imports System.Web
Imports System.Web.UI
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Extensibility
Imports ITHit.WebDAV.Server.CalDav

''' <summary>
''' This handler processes GET and HEAD requests to folders returning custom HTML page.
''' </summary>
Friend Class MyCustomGetHandler
    Implements IMethodHandlerAsync

    ''' <summary>
    ''' Handler for GET and HEAD request registered with the engine before registering this one.
    ''' We call this default handler to handle GET and HEAD for files, because this handler
    ''' only handles GET and HEAD for folders.
    ''' </summary>
    Public Property OriginalHandler As IMethodHandlerAsync

    ''' <summary>
    ''' Gets a value indicating whether output shall be buffered to calculate content length.
    ''' Don't buffer output to calculate content length.
    ''' </summary>
    Public ReadOnly Property EnableOutputBuffering As Boolean Implements IMethodHandlerAsync.EnableOutputBuffering
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' Gets a value indicating whether engine shall log response data (even if debug logging is on).
    ''' </summary>
    Public ReadOnly Property EnableOutputDebugLogging As Boolean Implements IMethodHandlerAsync.EnableOutputDebugLogging
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' Gets a value indicating whether the engine shall log request data.
    ''' </summary>
    Public ReadOnly Property EnableInputDebugLogging As Boolean Implements IMethodHandlerAsync.EnableInputDebugLogging
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' Path to the folder where HTML files are located.
    ''' </summary>
    Private ReadOnly htmlPath As String

    ''' <summary>
    ''' Creates instance of this class.
    ''' </summary>
    ''' <param name="contentRootPathFolder">Path to the folder where HTML files are located.</param>
    Public Sub New(contentRootPathFolder As String)
        Me.htmlPath = contentRootPathFolder
    End Sub

    Public Async Function ProcessRequestAsync(context As DavContextBaseAsync, item As IHierarchyItemAsync) As Task Implements IMethodHandlerAsync.ProcessRequestAsync
        If TypeOf item Is IItemCollectionAsync Then
            Await context.EnsureBeforeResponseWasCalledAsync()
            If context.Request.RawUrl.EndsWith("?connect") Then
                Await WriteProfileAsync(context, item, htmlPath)
                Return
            End If

            Dim page As IHttpAsyncHandler = CType(System.Web.Compilation.BuildManager.CreateInstanceFromVirtualPath("~/MyCustomHandlerPage.aspx", GetType(MyCustomHandlerPage)), IHttpAsyncHandler)
            If Type.GetType("Mono.Runtime") IsNot Nothing Then
                page.ProcessRequest(HttpContext.Current)
            Else
                Await Task.Factory.FromAsync(AddressOf page.BeginProcessRequest, AddressOf page.EndProcessRequest, HttpContext.Current, Nothing)
            End If
        Else
            Await OriginalHandler.ProcessRequestAsync(context, item)
        End If
    End Function

    Public Function AppliesTo(item As IHierarchyItemAsync) As Boolean Implements IMethodHandlerAsync.AppliesTo
        Return TypeOf item Is IFolderAsync OrElse OriginalHandler.AppliesTo(item)
    End Function

    Private Async Function WriteProfileAsync(context As DavContextBaseAsync, item As IHierarchyItemAsync, htmlPath As String) As Task
        Dim mobileconfigFileName As String = Nothing
        Dim decription As String = Nothing
        If TypeOf item Is ICalendarFolderAsync Then
            mobileconfigFileName = "CalDAV.AppleProfileTemplete.mobileconfig"
            decription = TryCast(item, ICalendarFolderAsync).CalendarDescription
        End If

        decription = If(Not String.IsNullOrEmpty(decription), decription, item.Name)
        Dim templateContent As String = Nothing
        Using reader As TextReader = New StreamReader(Path.Combine(htmlPath, mobileconfigFileName))
            templateContent = Await reader.ReadToEndAsync()
        End Using

        Dim url As Uri = New Uri(context.Request.UrlPrefix)
        Dim payloadUUID As String = item.Path.Split({"/"c}, StringSplitOptions.RemoveEmptyEntries).Last()
        Dim profile As String = String.Format(templateContent,
                                             url.Host, ' host name
                                             item.Path, ' CalDAV / CardDAV Principal URL. Here we can return (await (item as ICurrentUserPrincipalAsync).GetCurrentUserPrincipalAsync()).Path if needed.
                                             TryCast(context, DavContext).UserName, ' user name
                                             url.Port, ' port                
                                             (url.Scheme = "https").ToString().ToLower(), decription, ' CardDAV / CardDAV Account Description
                                             Assembly.GetAssembly(Me.GetType()).GetName().Version.ToString(), Assembly.GetAssembly(GetType(DavEngineAsync)).GetName().Version.ToString(), payloadUUID
                                             )
        Dim profileBytes As Byte() = SignProfile(context, profile)
        context.Response.ContentType = "application/x-apple-aspen-config"
        context.Response.AddHeader("Content-Disposition", "attachment; filename=profile.mobileconfig")
        context.Response.ContentLength = profileBytes.Length
        Using writer As BinaryWriter = New BinaryWriter(context.Response.OutputStream)
            writer.Write(profileBytes)
        End Using
    End Function

    Private Function SignProfile(context As DavContextBaseAsync, profile As String) As Byte()
        Return context.Engine.ContentEncoding.GetBytes(profile)
    End Function
End Class
