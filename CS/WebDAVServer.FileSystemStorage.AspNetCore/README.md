
<h1 class="d-xl-block d-none">Cross-platform ASP.NET Core WebDAV Server Sample with File System Back-End</h1>
<p>This is a cross-platform WebDAV server sample with a file system back-end that stores all data in a file system and runs as ASP.NET Core web application on Windows, OS X, and Linux. The sample keeps locks and custom properties in NTFS Alternate Data File Streams in the case of Windows or in Extended Attributes in the case of OS X and Linux. This sample is anonymous by default but is provided with sample Basic and Digest authentication middle-ware.</p>
<p>This sample is provided with a sample web page that demonstrates <a title="AJAX Library" href="https://www.webdavsystem.com/ajax/">WebDAV Ajax Library</a> integration that is used to open documents from a web page, list server content and navigate folders structure.</p>
<p><span class="warn">This sample is provided as part of the IT Hit WebDAV Server Engine v5 and later and is published to <a href="https://github.com/ITHit/WebDAVServerSamples/">GitHub</a> and to&nbsp;<a href="https://www.nuget.org/packages/WebDAVServer.FileSystemStorage">NuGet</a>. For a legacy file system storage sample provided with IT Hit WebDAV Server Engine v4.5 based on HttpHandler see <a title="WebDAV File System" href="https://www.webdavsystem.com/server/server_examples/ntfs_storage_file_system/">this article</a>.<br></span></p>
<h2>Prerequisites</h2>
<ul>
<li>.NET Core 3.1 or later version on Windows, OS X or Linux.</li>
<li>A file system with Extended Attributes support: NTFS, Ext2-Ext4, HFS+, APFS, etc.</li>
</ul>
<h2>Running Sample Using Command Prompt</h2>
<p>The instructions below&nbsp;describe how to install a sample from NuGet and are universal for Windows, OS X and Linux:</p>
<ol>
<li>Install .NET Core. Follow instructions on this page to install Microsoft .NET Core on Windows, OS X and Linux:&nbsp;<a href="https://www.microsoft.com/net/core">https://www.microsoft.com/net/core</a></li>
<li>Install NuGet client tools: <a href="https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools">https://docs.microsoft.com/en-us/nuget/install-nuget-client-tools</a></li>
<li>Create a project folder, for example,<code class="code">/WebDAVServer/</code>, and navigate to that folder in console.</li>
<li><span><span>Get the project from NuGet:&nbsp;</span></span>
<pre class="brush:html;auto-links:false;gutter:false;toolbar:false;first-line:">nuget install WebDAVServer.FileSystemStorage</pre>
</li>
<li><span>Navigate to the project folder, <span>where the .scproj file is located</span></span><span><span>:</span></span>
<pre class="brush:html;auto-links:false;toolbar:false">cd WebDAVServer.FileSystemStorage.&lt;version&gt;</pre>
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
<p style="text-align: center;"></p>
<h2><span>Setting the License</span></h2>
<p><span><span>To run the example, you will need a valid IT Hit WebDAV Server Engine License. You can download the license in </span>the <a title="Download" href="https://www.webdavsystem.com/server/download/">product download area</a>.<span> Note that the Engine is fully functional with a trial license and does not have any limitations. The trial license is valid for one month and the engine will stop working after this. You can check the expiration date inside the license file. <span>Download the license file and specify it's content in <code class="code">License</code> field under <code class="code">DavEngineOptions</code> in <code class="code">appsettings.webdav.json</code> file.</span><br></span></span></p>
<p>You can also run the sample <span>without explicitly specifying a license </span>for 5 days. In this case,&nbsp;<span>the&nbsp;</span>Engine will automatically request the trial license from the IT Hit website https://www.webdavsystem.com. Make sure it is accessible via firewalls if any. After 5 days the Engine will stop working. To extend the trial period you will need to download a license in a&nbsp;<span><a title="Download" href="https://www.webdavsystem.com/server/download/">product download area</a></span> and specify it in <span><span><code class="code">appsettings.webdav.json</code></span></span></p>
<h2>Storage location</h2>
<p><span>By default, files are stored in the file&nbsp;system in&nbsp;</span><code class="code">\App_Data\WebDav\Storage\</code>&nbsp;<span>folder. You can change the storage location in <code class="code">RepositoryPath</code> in <span><span><span><code class="code">appsettings.webdav.json</code></span></span></span>. You can specify either an absolute or relative path.<br></span></p>
<h2><span>Locks and Custom Properties Location</span></h2>
<p><span>By default locks and custom properties are stored in Alternate Data Streams (on Windows) or in Extended Attributes (on Linux and Mac), together with a file. If Alternate Data Streams/Extended Attributes are not supported, this sample will try to store locks and custom properties in the system temp folder. </span></p>
<p><span>To specify a location where the locks and custom properties should be stored specify the folder path in the&nbsp;<code class="code">AttrStoragePath</code>&nbsp;in&nbsp;<code class="code">appsettings.webdav.json</code>. Every attribute will be stored in a separate file under this folder in this case.<br><span>// "AttrStoragePath": "App_Data/WebDAV/Attributes/"</span></span></p>
<h2><span>Authentication</span></h2>
<p><span>This sample is provided with Basic and Digest authentication middle-ware. By default the authentication is disabled and the sample is anonymous. To enable Basic or Digest authentication uncomment the <code class="code">UseBasicAuth()</code> or <code class="code">UseDigesAuth()</code> calls inside <code class="code">Configure()</code> method in <code class="code">Startup.cs</code>.</span></p>
<p><span>For the sake of simplicity, credentials are stored in&nbsp;appsettings.webdav.json file.</span></p>
<p><span> <!--StartFragment--><span class="warn strong-warn"> Microsoft Office on Windows and OS X as well as Windows Shell (Web Folders / <a title="On Windows" href="https://www.webdavsystem.com/server/access/windows/">mini-redirector</a>), requires secure SSL connection when used with Basic authentication. Microsoft Office will fail to open a document via an insecure connection with Basic authentication. For a workaround please see the following articles: <br> - In the case of MS Office on Windows:&nbsp;<a href="http://support.microsoft.com/kb/2123563">You cannot open Office file types directly from a server that only supports Basic Authentication over a non-SSL connection with Office applications</a>. <br> - In the case of MS Office on Mac OS X:&nbsp;<a href="https://support.microsoft.com/en-us/kb/2498069">You cannot open Office for Mac files directly from a server that supports only Basic authentication over a non-SSL connection</a>.</span><!--EndFragment--> </span></p>
<h3>See Also:</h3>
<ul>
<li><a href="https://www.webdavsystem.com/server/server_examples/running_webdav_samples/">Running the WebDAV Samples</a></li>
<li><a href="https://www.webdavsystem.com/server/server_examples/running_webdav_samples/">WebDAV Server Samples Problems and Troubleshooting</a></li>
</ul>
<h3 class="para d-inline next-article-heading">Next Article:</h3>
<a title="ASP.NET Core WebDAV Server Sample with Azure Data Lake Storage Back-End" href="https://www.webdavsystem.com/server/server_examples/azure_blob_data_lake/">ASP.NET Core WebDAV Server Sample with Azure Blob / Data Lake Storage Back-End</a>

