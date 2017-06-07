Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Linq
Imports System.Text
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
            Using reader As TextReader = File.OpenText(Path.Combine(htmlPath, "MyCustomHandlerPage.html"))
                Dim html As String = Await reader.ReadToEndAsync()
                html = html.Replace("_webDavServerRoot_", context.Request.ApplicationPath.TrimEnd("/"c))
                html = html.Replace("_webDavServerVersion_",
                                   GetType(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString())
                Await WriteHtmlAsync(context, html)
            End Using
        ElseIf context.Request.RawUrl.StartsWith("/AjaxFileBrowser/") Then
            Await context.EnsureBeforeResponseWasCalledAsync()
            Dim filePath As String = Path.Combine(htmlPath, context.Request.RawUrl.TrimStart("/"c).Replace("/"c, Path.DirectorySeparatorChar))
            Dim queryIndex As Integer = filePath.LastIndexOf("?"c)
            If queryIndex > -1 Then
                filePath = filePath.Remove(queryIndex)
            End If

            If Not File.Exists(filePath) Then
                Throw New DavException("File not found: " & filePath, DavStatus.NOT_FOUND)
            End If

            Using reader As TextReader = File.OpenText(filePath)
                Dim html As String = Await reader.ReadToEndAsync()
                Await WriteHtmlAsync(context, html)
            End Using
        Else
            Await OriginalHandler.ProcessRequestAsync(context, item)
        End If
    End Function

    Private Async Function WriteHtmlAsync(context As DavContextBaseAsync, html As String) As Task
        Dim encoding As Encoding = context.Engine.ContentEncoding
        context.Response.ContentLength = encoding.GetByteCount(html)
        context.Response.ContentType = String.Format("text/html; charset={0}", encoding.WebName)
        If context.Request.HttpMethod = "GET" Then
            Using writer = New StreamWriter(context.Response.OutputStream, encoding)
                Await writer.WriteAsync(html)
            End Using
        End If
    End Function

    Public Function AppliesTo(item As IHierarchyItemAsync) As Boolean Implements IMethodHandlerAsync.AppliesTo
        Return TypeOf item Is IFolderAsync OrElse OriginalHandler.AppliesTo(item)
    End Function
End Class
