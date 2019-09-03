Imports System
Imports System.Collections.Generic
Imports System.IO
Imports System.Reflection
Imports System.Linq
Imports System.Text
Imports Microsoft.Win32
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

    ''' <summary>
    ''' Handles GET and HEAD request.
    ''' </summary>
    ''' <param name="context">Instace of <see cref="ContextBaseAsync"/> .</param>
    ''' <param name="item">Instance of <see cref="IHierarchyItemAsync"/>  which was returned by
    ''' <see cref="ContextBaseAsync.GetHierarchyItemAsync"/>  for this request.</param>
    Public Async Function ProcessRequestAsync(context As ContextBaseAsync, item As IHierarchyItemBaseAsync) As Task Implements IMethodHandlerAsync.ProcessRequestAsync
        Dim urlPath As String = context.Request.RawUrl.Substring(context.Request.ApplicationPath.TrimEnd("/"c).Length)
        If TypeOf item Is IItemCollectionAsync Then
            ' In case of GET requests to WebDAV folders we serve a web page to display 
            ' any information about this server and how to use it.
            ' Remember to call EnsureBeforeResponseWasCalledAsync here if your context implementation
            ' makes some useful things in BeforeResponseAsync.
            Await context.EnsureBeforeResponseWasCalledAsync()
            Dim htmlName As String = "MyCustomHandlerPage.html"
            Using reader As TextReader = File.OpenText(Path.Combine(htmlPath, htmlName))
                Dim html As String = Await reader.ReadToEndAsync()
                html = html.Replace("_webDavServerRoot_", context.Request.ApplicationPath.TrimEnd("/"c))
                html = html.Replace("_webDavServerVersion_",
                                   GetType(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString())
                Await WriteFileContentAsync(context, html, htmlName)
            End Using
        ElseIf urlPath.StartsWith("/AjaxFileBrowser/") OrElse urlPath.StartsWith("/wwwroot/") Then
            ' The "/AjaxFileBrowser/" are not a WebDAV folders. They can be used to store client script files, 
            ' images, static HTML files or any other files that does not require access via WebDAV.
            ' Any request to the files in this folder will just serve them to the client. 
            Await context.EnsureBeforeResponseWasCalledAsync()
            Dim filePath As String = Path.Combine(htmlPath, urlPath.TrimStart("/"c).Replace("/"c, Path.DirectorySeparatorChar))
            ' Remove query string.
            Dim queryIndex As Integer = filePath.LastIndexOf("?"c)
            If queryIndex > -1 Then
                filePath = filePath.Remove(queryIndex)
            End If

            If Not File.Exists(filePath) Then
                Throw New DavException("File not found: " & filePath, DavStatus.NOT_FOUND)
            End If

            Dim encoding As Encoding = context.Engine.ContentEncoding
            context.Response.ContentType = String.Format("{0}; charset={1}", If(MimeType.GetMimeType(Path.GetExtension(filePath)), "application/octet-stream"), encoding.WebName)
            ' Return file content in case of GET request, in case of HEAD just return headers.
            If context.Request.HttpMethod = "GET" Then
                Using fileStream As FileStream = File.OpenRead(filePath)
                    context.Response.ContentLength = fileStream.Length
                    Await fileStream.CopyToAsync(context.Response.OutputStream)
                End Using
            End If
        Else
            Await OriginalHandler.ProcessRequestAsync(context, item)
        End If
    End Function

    ''' <summary>
    ''' Writes HTML to the output stream in case of GET request using encoding specified in Engine. 
    ''' Writes headers only in case of HEAD request.
    ''' </summary>
    ''' <param name="context">Instace of <see cref="ContextBaseAsync"/> .</param>
    ''' <param name="content">String representation of the content to write.</param>
    ''' <param name="filePath">Relative file path, which holds the content.</param>
    Private Async Function WriteFileContentAsync(context As ContextBaseAsync, content As String, filePath As String) As Task
        Dim encoding As Encoding = context.Engine.ContentEncoding
        context.Response.ContentLength = encoding.GetByteCount(content)
        context.Response.ContentType = String.Format("{0}; charset={1}", If(MimeType.GetMimeType(Path.GetExtension(filePath)), "application/octet-stream"), encoding.WebName)
        ' Return file content in case of GET request, in case of HEAD just return headers.
        If context.Request.HttpMethod = "GET" Then
            Using writer = New StreamWriter(context.Response.OutputStream, encoding)
                Await writer.WriteAsync(content)
            End Using
        End If
    End Function

    ''' <summary>
    ''' This handler shall only be invoked for <see cref="IFolderAsync"/>  items or if original handler (which
    ''' this handler substitutes) shall be called for the item.
    ''' </summary>
    ''' <param name="item">Instance of <see cref="IHierarchyItemAsync"/>  which was returned by
    ''' <see cref="ContextBaseAsync.GetHierarchyItemAsync"/>  for this request.</param>
    ''' <returns>Returns <c>true</c> if this handler can handler this item.</returns>
    Public Function AppliesTo(item As IHierarchyItemBaseAsync) As Boolean Implements IMethodHandlerAsync.AppliesTo
        Return TypeOf item Is IFolderAsync OrElse OriginalHandler.AppliesTo(item)
    End Function
End Class
