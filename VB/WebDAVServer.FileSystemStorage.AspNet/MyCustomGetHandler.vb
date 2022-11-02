Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Linq
Imports System.Text
Imports System.Web
Imports System.Web.UI
Imports System.Threading.Tasks
Imports ITHit.WebDAV.Server
Imports ITHit.WebDAV.Server.Class1
Imports ITHit.WebDAV.Server.Extensibility
Imports ITHit.Server.Extensibility
Imports ITHit.Server

''' <summary>
''' This handler processes GET and HEAD requests to folders returning custom HTML page.
''' </summary>
Friend Class MyCustomGetHandler
    Implements IMethodHandler(Of IHierarchyItem)

    ''' <summary>
    ''' Handler for GET and HEAD request registered with the engine before registering this one.
    ''' We call this default handler to handle GET and HEAD for files, because this handler
    ''' only handles GET and HEAD for folders.
    ''' </summary>
    Public Property OriginalHandler As IMethodHandler(Of IHierarchyItem)

    ''' <summary>
    ''' Gets a value indicating whether output shall be buffered to calculate content length.
    ''' Don't buffer output to calculate content length.
    ''' </summary>
    Public ReadOnly Property EnableOutputBuffering As Boolean Implements IMethodHandler(Of IHierarchyItem).EnableOutputBuffering
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' Gets a value indicating whether engine shall log response data (even if debug logging is on).
    ''' </summary>
    Public ReadOnly Property EnableOutputDebugLogging As Boolean Implements IMethodHandler(Of IHierarchyItem).EnableOutputDebugLogging
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' Gets a value indicating whether the engine shall log request data.
    ''' </summary>
    Public ReadOnly Property EnableInputDebugLogging As Boolean Implements IMethodHandler(Of IHierarchyItem).EnableInputDebugLogging
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
    ''' <param name="context">Instace of <see cref="ContextAsync{IHierarchyItem}"/> .</param>
    ''' <param name="item">Instance of <see cref="IHierarchyItem"/>  which was returned by
    ''' <see cref="ContextAsync{IHierarchyItem}.GetHierarchyItemAsync"/>  for this request.</param>
    Public Async Function ProcessRequestAsync(context As ContextAsync(Of IHierarchyItem), item As IHierarchyItem) As Task Implements IMethodHandler(Of IHierarchyItem).ProcessRequestAsync
        Dim urlPath As String = context.Request.RawUrl.Substring(context.Request.ApplicationPath.TrimEnd("/"c).Length)
        If TypeOf item Is IItemCollection Then
            ' In case of GET requests to WebDAV folders we serve a web page to display 
            ' any information about this server and how to use it.
            ' Remember to call EnsureBeforeResponseWasCalledAsync here if your context implementation
            ' makes some useful things in BeforeResponseAsync.
            Await context.EnsureBeforeResponseWasCalledAsync()
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
    ''' This handler shall only be invoked for <see cref="IFolder"/>  items or if original handler (which
    ''' this handler substitutes) shall be called for the item.
    ''' </summary>
    ''' <param name="item">Instance of <see cref="IHierarchyItem"/>  which was returned by
    ''' <see cref="ContextAsync{IHierarchyItem}.GetHierarchyItemAsync"/>  for this request.</param>
    ''' <returns>Returns <c>true</c> if this handler can handler this item.</returns>
    Public Function AppliesTo(item As IHierarchyItem) As Boolean Implements IMethodHandler(Of IHierarchyItem).AppliesTo
        Return TypeOf item Is IFolder OrElse OriginalHandler.AppliesTo(item)
    End Function
End Class
