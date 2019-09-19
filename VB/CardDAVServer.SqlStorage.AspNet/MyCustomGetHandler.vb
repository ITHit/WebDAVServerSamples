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
Imports ITHit.WebDAV.Server.CardDav
Imports ITHit.Server.Extensibility
Imports ITHit.Server

''' <summary>
''' This handler processes GET and HEAD requests to folders returning custom HTML page.
''' </summary>
Friend Class MyCustomGetHandler
    Implements IMethodHandlerAsync(Of IHierarchyItemAsync)

    ''' <summary>
    ''' Handler for GET and HEAD request registered with the engine before registering this one.
    ''' We call this default handler to handle GET and HEAD for files, because this handler
    ''' only handles GET and HEAD for folders.
    ''' </summary>
    Public Property OriginalHandler As IMethodHandlerAsync(Of IHierarchyItemAsync)

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

    ''' <summary>
    ''' Handles GET and HEAD request.
    ''' </summary>
    ''' <param name="context">Instace of <see cref="ContextAsync{IHierarchyItemAsync}"/> .</param>
    ''' <param name="item">Instance of <see cref="IHierarchyItemAsync"/>  which was returned by
    ''' <see cref="ContextAsync{IHierarchyItemAsync}.GetHierarchyItemAsync"/>  for this request.</param>
    Public Async Function ProcessRequestAsync(context As ContextAsync(Of IHierarchyItemAsync), item As IHierarchyItemAsync) As Task Implements IMethodHandlerAsync.ProcessRequestAsync
        Dim urlPath As String = context.Request.RawUrl.Substring(context.Request.ApplicationPath.TrimEnd("/"c).Length)
        If TypeOf item Is IItemCollectionAsync Then
            ' In case of GET requests to WebDAV folders we serve a web page to display 
            ' any information about this server and how to use it.
            ' Remember to call EnsureBeforeResponseWasCalledAsync here if your context implementation
            ' makes some useful things in BeforeResponseAsync.
            Await context.EnsureBeforeResponseWasCalledAsync()
            ' Request to iOS/OS X CalDAV/CardDAV profile.
            If context.Request.RawUrl.EndsWith("?connect") Then
                Await WriteProfileAsync(context, item, htmlPath)
                Return
            End If

            Dim page As IHttpAsyncHandler = CType(System.Web.Compilation.BuildManager.CreateInstanceFromVirtualPath("~/MyCustomHandlerPage.aspx", GetType(MyCustomHandlerPage)), IHttpAsyncHandler)
            If Type.GetType("Mono.Runtime") IsNot Nothing Then
                page.ProcessRequest(HttpContext.Current)
            Else
                ' Here we call BeginProcessRequest instead of ProcessRequest to start an async page execution and be able to call RegisterAsyncTask if required. 
                ' To call APM method (Begin/End) from TAP method (Task/async/await) the Task.FromAsync must be used.
                Await Task.Factory.FromAsync(AddressOf page.BeginProcessRequest, AddressOf page.EndProcessRequest, HttpContext.Current, Nothing)
            End If
        Else
            Await OriginalHandler.ProcessRequestAsync(context, item)
        End If
    End Function

    ''' <summary>
    ''' This handler shall only be invoked for <see cref="IFolderAsync"/>  items or if original handler (which
    ''' this handler substitutes) shall be called for the item.
    ''' </summary>
    ''' <param name="item">Instance of <see cref="IHierarchyItemAsync"/>  which was returned by
    ''' <see cref="ContextAsync{IHierarchyItemAsync}.GetHierarchyItemAsync"/>  for this request.</param>
    ''' <returns>Returns <c>true</c> if this handler can handler this item.</returns>
    Public Function AppliesTo(item As IHierarchyItemAsync) As Boolean Implements IMethodHandlerAsync.AppliesTo
        Return TypeOf item Is IFolderAsync OrElse OriginalHandler.AppliesTo(item)
    End Function

    ''' <summary>
    ''' Writes iOS / OS X CalDAV/CardDAV profile.
    ''' </summary>
    ''' <param name="context">Instace of <see cref="ContextAsync{IHierarchyItemAsync}"/> .</param>
    ''' <param name="item">ICalendarFolderAsync or IAddressbookFolderAsync item.</param>
    ''' <returns></returns>
    Private Async Function WriteProfileAsync(context As ContextAsync(Of IHierarchyItemAsync), item As IHierarchyItemBaseAsync, htmlPath As String) As Task
        Dim mobileconfigFileName As String = Nothing
        Dim decription As String = Nothing
        If TypeOf item Is IAddressbookFolderAsync Then
            mobileconfigFileName = "CardDAV.AppleProfileTemplete.mobileconfig"
            decription = TryCast(item, IAddressbookFolderAsync).AddressbookDescription
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
                                             TryCast(context, DavContext).Identity.Name, ' user name
                                             url.Port, ' port                
                                             (url.Scheme = "https").ToString().ToLower(), ' SSL
                                             decription, ' CardDAV / CardDAV Account Description
                                             Assembly.GetAssembly(Me.GetType()).GetName().Version.ToString(),
                                             Assembly.GetAssembly(GetType(DavEngineAsync)).GetName().Version.ToString(),
                                             payloadUUID
                                             )
        Dim profileBytes As Byte() = SignProfile(context, profile)
        context.Response.ContentType = "application/x-apple-aspen-config"
        context.Response.AddHeader("Content-Disposition", "attachment; filename=profile.mobileconfig")
        context.Response.ContentLength = profileBytes.Length
        Using writer As BinaryWriter = New BinaryWriter(context.Response.OutputStream)
            writer.Write(profileBytes)
        End Using
    End Function

    ''' <summary>
    ''' Signs iOS / OS X payload profile with SSL certificate.
    ''' </summary>
    ''' <param name="context">Instace of <see cref="ContextAsync{IHierarchyItemAsync}"/> .</param>
    ''' <param name="profile">Profile to sign.</param>
    ''' <returns>Signed profile.</returns>
    Private Function SignProfile(context As ContextAsync(Of IHierarchyItemAsync), profile As String) As Byte()
        ' Here you will sign your profile with SSL certificate to avoid "Unsigned" warning on iOS and OS X.
        ' For demo purposes we just return the profile content unmodified.
        Return context.Engine.ContentEncoding.GetBytes(profile)
    End Function
End Class
