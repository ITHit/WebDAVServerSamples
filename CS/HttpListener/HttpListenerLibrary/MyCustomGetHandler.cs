using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using ITHit.WebDAV.Server;
using ITHit.WebDAV.Server.Class1;
using ITHit.WebDAV.Server.Extensibility;
using System;
using Microsoft.AspNetCore.StaticFiles;

namespace HttpListenerLibrary
{
    /// <summary>
    /// This handler processes GET and HEAD requests to folders returning custom HTML page.
    /// </summary>
    public class MyCustomGetHandler : IMethodHandlerAsync
    {
        /// <summary>
        /// Handler for GET and HEAD request registered with the engine before registering this one.
        /// We call this default handler to handle GET and HEAD for files, because this handler
        /// only handles GET and HEAD for folders.
        /// </summary>
        public IMethodHandlerAsync OriginalHandler { get; set; }

        /// <summary>
        /// Gets a value indicating whether output shall be buffered to calculate content length.
        /// Don't buffer output to calculate content length.
        /// </summary>
        public bool EnableOutputBuffering
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether engine shall log response data (even if debug logging is on).
        /// </summary>
        public bool EnableOutputDebugLogging
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the engine shall log request data.
        /// </summary>
        public bool EnableInputDebugLogging
        {
            get { return false; }
        }

        /// <summary>
        /// Creates instance of this class.
        /// </summary>
        /// <param name="contentRootPathFolder">Path to the folder where HTML files are located.</param>
        public MyCustomGetHandler(string contentRootPathFolder)
        {
            getFileContent = async (relativeFilePath) =>
            {
                string filePath = Path.Combine(contentRootPathFolder, relativeFilePath);
                if (!File.Exists(filePath))
                {
                    throw new DavException("File not found: " + filePath, DavStatus.NOT_FOUND);
                }

                using (TextReader reader = File.OpenText(filePath))
                {
                    return await reader.ReadToEndAsync();
                }
            };
        }

        /// <summary>
        /// Creates instance of this class.
        /// </summary>
        /// <param name="getFileContentFunction">Function which retrieves file content by path.</param>
        public MyCustomGetHandler(Func<string, Task<string>> getFileContentFunction)
        {
            getFileContent = getFileContentFunction;
        }

        /// <summary>
        /// Reads file content by file path. Depending on OS reads file content from 
        /// Win/Linux/iOS/OSX file system or from Android assets.
        /// </summary>
        /// <exception cref="DavException">If file not found.</exception>
        private Func<string, Task<string>> getFileContent;

        /// <summary>
        /// Handles GET and HEAD request.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="item">Instance of <see cref="IHierarchyItemAsync"/> which was returned by
        /// <see cref="DavContextBaseAsync.GetHierarchyItemAsync"/> for this request.</param>
        public async Task ProcessRequestAsync(DavContextBaseAsync context, IHierarchyItemAsync item)
        {
            if (item is IItemCollectionAsync)
            {
                // In case of GET requests to WebDAV folders we serve a web page to display 
                // any information about this server and how to use it.

                // Remember to call EnsureBeforeResponseWasCalledAsync here if your context implementation
                // makes some useful things in BeforeResponseAsync.
                await context.EnsureBeforeResponseWasCalledAsync();

                string htmlName = "MyCustomHandlerPage.html";
                string html = await getFileContent(htmlName);
                html = html.Replace("_webDavServerRoot_", context.Request.ApplicationPath.TrimEnd('/'));
                html = html.Replace("_webDavServerVersion_",
                    typeof(DavEngineAsync).GetTypeInfo().Assembly.GetName().Version.ToString());

                await WriteFileContentAsync(context, html, htmlName);
            }
            else if (context.Request.RawUrl.StartsWith("/wwwroot/"))
            {
                // The "/wwwroot/" is not a WebDAV folder. It can be used to store client script files, 
                // images, static HTML files or any other files that does not require access via WebDAV.
                // Any request to the files in this folder will just serve them to client. 

                await context.EnsureBeforeResponseWasCalledAsync();
                string relativeFilePath = context.Request.RawUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);

                // Remove query string.
                int queryIndex = relativeFilePath.LastIndexOf('?');
                if (queryIndex > -1)
                {
                    relativeFilePath = relativeFilePath.Remove(queryIndex);
                }
                await WriteFileContentAsync(context, await getFileContent(relativeFilePath), relativeFilePath);
            }
            else
            {
                await OriginalHandler.ProcessRequestAsync(context, item);
            }
        }

        /// <summary>
        /// Writes HTML to the output stream in case of GET request using encoding specified in Engine. 
        /// Writes headers only in caes of HEAD request.
        /// </summary>
        /// <param name="context">Instace of <see cref="DavContextBaseAsync"/>.</param>
        /// <param name="content">String representation of the content to write.</param>
        /// <param name="filePath">Relative file path, which holds the content.</param>
        private async Task WriteFileContentAsync(DavContextBaseAsync context, string content, string filePath)
        {
            Encoding encoding = context.Engine.ContentEncoding; // UTF-8 by default
            context.Response.ContentLength = encoding.GetByteCount(content);
            if(new FileExtensionContentTypeProvider().TryGetContentType(filePath, out string contentType))
            {
                context.Response.ContentType = $"{contentType}; charset={encoding.WebName}";
            }

            // Return file content in case of GET request, in case of HEAD just return headers.
            if (context.Request.HttpMethod == "GET")
            {
                using (var writer = new StreamWriter(context.Response.OutputStream))
                {
                    await writer.WriteAsync(content);
                }
            }
        }

        /// <summary>
        /// This handler shall only be invoked for <see cref="IFolderAsync"/> items or if original handler (which
        /// this handler substitutes) shall be called for the item.
        /// </summary>
        /// <param name="item">Instance of <see cref="IHierarchyItemAsync"/> which was returned by
        /// <see cref="DavContextBaseAsync.GetHierarchyItemAsync"/> for this request.</param>
        /// <returns>Returns <c>true</c> if this handler can handler this item.</returns>
        public bool AppliesTo(IHierarchyItemAsync item)
        {
            return item is IFolderAsync || OriginalHandler.AppliesTo(item);
        }
    }
}