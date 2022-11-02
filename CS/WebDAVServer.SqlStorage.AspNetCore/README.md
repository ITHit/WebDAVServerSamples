
<h1 class="d-xl-block d-none">Cross-platform ASP.NET Core WebDAV Server Sample with Microsoft SQL Back-End</h1>
<p>This is a cross-platform WebDAV server sample with Microsoft SQL back-end that stores all data in a file system and runs as ASP.NET Core web application on Windows, OS X and Linux. The sample keeps all data including locks, file content and custom properties in the Microsoft SQL Server database. The&nbsp;WebDAVServer.NetCore.SqlStorage example is a fully-functional WebDAV server that can be used to open, edit and save Microsoft Office documents directly on a server, without download/upload steps. You can use it to access documents using Microsoft Mini-Redirector/Web Folders, Mac OS X Finder or any other WebDAV client. This sample is anonymous by default but is provided with sample Basic and Digest authentication middle-ware.</p>
<p>This sample is provided with a sample web page that demonstrates <a title="AJAX Library" href="https://www.webdavsystem.com/ajax/">WebDAV Ajax Library</a> integration that is used to open documents from a web page, list server content and navigate folders structure.</p>
<p><span class="warn">This sample is provided as part of the IT Hit WebDAV Server Engine v5 and later and is published to <a href="https://github.com/ITHit/WebDAVServerSamples/">GitHub</a> and to&nbsp;<a href="https://www.nuget.org/packages/WebDAVServer.SqlStorage/">NuGet</a>. For a legacy, SQL Storage sample provided with IT Hit WebDAV Server Engine v4.5 and earlier based on HttpHandler see <a title="WebDAV SQL" href="https://www.webdavsystem.com/server/server_examples/sql_storage/">this article</a>.<br></span></p>
<h2>Prerequisites</h2>
<ul>
<li>.NET Core 3.1 or later version on Windows, OS X or Linux.</li>
<li>Microsoft SQL Server Express LocalDB or Microsoft SQL Express&nbsp;or Microsoft SQL Server 2019 / 2016 / 2014 / 2012 / 2008 / 2005.</li>
</ul>
<h2>Running Sample Using Command Prompt</h2>
<p>The instructions below&nbsp;describe how to install a sample from NuGet and are universal for Windows, OS X and Linux:</p>
<ol>
<li>Install .NET Core. Follow instructions on this page to install Microsoft .NET Core on Windows, OS X, and Linux:&nbsp;<a href="https://www.microsoft.com/net/core">https://www.microsoft.com/net/core</a></li>
<li>Install NuGet client tools: <a href="https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools">https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools</a></li>
<li>Create a project folder, for example,<code class="code">/WebDAVServer/</code>, and navigate to that folder in console.</li>
<li><span><span>Get the project from NuGet:&nbsp;</span></span>
<pre class="brush:html;auto-links:false;gutter:false;toolbar:false;first-line:">nuget install WebDAVServer.SqlStorage</pre>
</li>
<li><span>Navigate to the project folder, where the .scproj file file is located</span>:
<pre class="brush:html;auto-links:false;gutter:false;toolbar:false">cd WebDAVServer.FileSystemStorage.&lt;version&gt;</pre>
</li>
<li><span><span>Restore dependencies:</span></span>
<pre class="brush:html;auto-links:false;gutter:false;toolbar:false">dotnet restore</pre>
</li>
<li><span><span>Build the project:</span></span>
<pre class="brush:html;auto-links:false;gutter:false;toolbar:false">dotnet build</pre>
</li>
<li><span><span>Run the project:</span></span>
<pre class="brush:html;auto-links:false;gutter:false;toolbar:false">dotnet run</pre>
</li>
</ol>
<p><span>Now your server runs on&nbsp;http://localhost:5000. Open the web browser with this URL and you will see the web page that lists folder content on your server and provides an edit button/link for opening each file from the server using WebDAV Ajax Library. You can also run the Ajax Integration test to find if your server is running correctly. To upload documents, create folders, copy and move files, manage custom properties, etc you can use the Browse Using Ajax File browser button.</span></p>
<h2><span>Setting the License</span></h2>
<p><span><span>To run the example, you will need a valid IT Hit WebDAV Server Engine License. You can download the license in </span>the <a title="Download" href="https://www.webdavsystem.com/server/download/">product download area</a>.<span> Note that the Engine is fully functional with a trial license and does not have any limitations. The trial license is valid for one month and the engine will stop working after this. You can check the expiration date inside the license file. <span>Download the license file and specify it's content in <code class="code">License</code> field under <code class="code">DavEngineOptions</code> in <code class="code">appsettings.webdav.json</code> file.</span><br></span></span></p>
<p>You can also run the sample <span>without explicitly specifying a license </span>for 5 days. In this case,&nbsp;<span>the&nbsp;</span>Engine will automatically request the trial license from IT Hit website https://www.webdavsystem.com. Make sure it is accessible via firewalls if any. After 5 days the Engine will stop working. To extend the trial period you will need to download a license in a&nbsp;<span><a title="Download" href="https://www.webdavsystem.com/server/download/">product download area</a></span> and specify it in <span><span><code class="code">appsettings.webdav.json</code></span></span></p>
<h2>Storage location</h2>
<p><span>When you run the sample for the first time it will find a default database Microsoft SQL instance and runs the script from DB.sql to create a new WebDAV database. It will also populate it with sample files and folders. You can also create the database manually running the <span>DB.sql</span> on the required Microsoft SQL instance and updating the connection string in <span><span><span><span><code class="code">appsettings.webdav.json</code></span></span></span></span> file.</span><span><br></span></p>
<h2><span>Authentication</span></h2>
<p><span>This sample is provided with Basic and Digest authentication middle-ware. By default the authentication is disabled and the sample is anonymous. To enable Basic or Digest authentication uncomment the <code class="code">UseBasicAuth()</code> or <code class="code">UseDigesAuth()</code> calls inside <code class="code">Configure()</code> method in <code class="code">Startup.cs</code>.</span></p>
<p><span>For the sake of simplicity, credentials are stored in&nbsp;appsettings.webdav.json file.</span></p>
<p><span> <span class="warn strong-warn">Microsoft Office on Windows and OS X as well as Windows Shell (Web Folders / <a title="On Windows" href="https://www.webdavsystem.com/server/access/windows/">mini-redirector</a>), requires secure SSL connection when used with Basic authentication. Microsoft Office will fail to open a document via an insecure connection with Basic authentication. For a workaround please see the following articles: <br> - In the case of MS Office on Windows:&nbsp;<a href="http://support.microsoft.com/kb/2123563">You cannot open Office file types directly from a server that only supports Basic Authentication over a non-SSL connection with Office applications</a>. <br> - In the case of MS Office on Mac OS X:&nbsp;<a href="https://support.microsoft.com/en-us/kb/2498069">You cannot open Office for Mac files directly from a server that supports only Basic authentication over a non-SSL connection</a>.</span> </span></p>
<h3>See Also:</h3>
<ul>
<li><a href="https://www.webdavsystem.com/server/server_examples/running_webdav_samples/">Running the WebDAV Samples</a></li>
<li><a href="https://www.webdavsystem.com/server/server_examples/running_webdav_samples/">WebDAV Server Samples Problems and Troubleshooting</a></li>
</ul>
<p><!--EndFragment--></p>
<h3 class="para d-inline next-article-heading">Next Article:</h3>
<a title="Cross-platform ASP.NET Core WebDAV Server Sample with File System Back-End" href="https://www.webdavsystem.com/server/server_examples/cross_platform_asp_net_core_file_system/">Cross-platform ASP.NET Core WebDAV Server Sample with File System Back-End</a>

