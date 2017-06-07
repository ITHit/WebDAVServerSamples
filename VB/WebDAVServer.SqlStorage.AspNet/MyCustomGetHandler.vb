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
End Class
