
<h1 class="d-xl-block d-none">ASP.NET Core WebDAV Server Sample with Azure Blob / Data Lake Storage Back-End</h1>
<p>This is a Class 2 WebDAV Server that keeps data in Azure Blob storage with Data Lake support. This sample publishes a hierarchical folder structure from Azure Data Lake and keeps locks, custom properties as well as file creation and modification dates in Azure Blob properties. It is using Azure AD authentication and <a title="Full-Text Search" href="https://www.webdavsystem.com/server/server_examples/azure_blob_data_lake/cognitive_search/">Azure Cognitive search</a> for the full-text indexing and search.</p>
<p><span class="warn">This sample can be created and deployed to your azure account automatically using the <a href="http://azure.webdavsystem.com/">Azure WebDAV Wizard</a>.</span></p>
<p><span>This sample is provided as part of the IT Hit WebDAV Server Engine v10.1 samples and later. You can download it in the product download area <a title="Download" href="https://www.webdavsystem.com/server/download/">here</a> and well as it is published to&nbsp;</span><a href="https://github.com/ITHit/WebDAVServerSamples/">GitHub</a><span></span><span>. To automatically update your Azure AD App Service running this sample you can use <a href="https://github.com/ITHit/WebDAVServer.AzureDataLakeStorage.AspNetCore">this repository</a>.</span></p>
<p><span>All locations contain identical code.</span></p>
<h2 class="heading-link" id="nav_prerequisites">Prerequisites</h2>
<ul>
<li>.NET Core 3.1 or later version on Windows, OS X, or Linux.</li>
<li>Microsoft Azure account.</li>
</ul>
<h2>Setting the License</h2>
<p>To run the example, you will need a valid IT Hit WebDAV Server Engine License. You can download the license in&nbsp;the&nbsp;<a title="Download" href="https://www.webdavsystem.com/server/download/">product download area</a>.&nbsp;Note that the Engine is fully functional with a trial license and does not have any limitations. The trial license is valid for one month and the engine will stop working after this. You can check the expiration date inside the license file.&nbsp;Download the license file and specify it's content in&nbsp;<code class="code">License</code>&nbsp;field under&nbsp;<code class="code">DavEngine</code>&nbsp;in&nbsp;<code class="code">appsettings.webdav.json</code>&nbsp;file.</p>
<p>You can also run the sample&nbsp;without explicitly specifying a license&nbsp;for 5 days. In this case,&nbsp;the&nbsp;Engine will automatically request the trial license from IT Hit website https://www.webdavsystem.com. Make sure it is accessible via firewalls if any. After 5 days the Engine will stop working. To extend the trial period you will need to download a license in a&nbsp;<a title="Download" href="https://www.webdavsystem.com/server/download/">product download area</a>&nbsp;and specify it in&nbsp;<code class="code">appsettings.webdav.json</code></p>
<h2>Creating an Azure Blob Storage with Data Lake Support</h2>
<ol>
<li>
<p>In your Microsoft Azure account go to <em>Storage Accounts</em>. Select <em>Add</em>:</p>
<p><img id="__mcenew" alt="In Microsoft Azure account go to Storage Accounts. Select Add." src="https://www.webdavsystem.com/media/1962/datalakestorageaccountadd.png" rel="118173">&nbsp;</p>
<p>Fill the <em>Storage account name</em>&nbsp;field, you will specify it in the config file:<img id="__mcenew" alt="Fill your Blob Storage account name field." src="https://www.webdavsystem.com/media/1966/blobstorageaccountcreate.png" rel="118189"></p>
<p>Go to the&nbsp;<em>Advanced</em> tab and enable the <em>Hierarchical namespace</em> under the <em>Data Lake Storage Gen2:</em></p>
<p><img id="__mcenew" alt="Enable the Hierarchical namespace under the Data Lake Storage Gen2 on the Advanced tab." src="https://www.webdavsystem.com/media/1965/datalakestoragehierarchicalnamespace.png" rel="118190"></p>
<p>&nbsp;Select <em>Review+Create</em>. Confirm the storage account creation.&nbsp;</p>
<p>Specify the storage account name in&nbsp;<code class="code">appsettings.webdav.json</code>&nbsp;as&nbsp;<code class="code">AzureStorageAccountName</code>&nbsp;setting value.</p>
</li>
<li>
<p>Go to <em>Containers</em> under the <em>Data Lake Storage</em>. Select <em>+Container</em> to create a new container. Fill the container name, you will specify it in the config file:</p>
<p><img id="__mcenew" alt="Create a new Data Lake Storage container" src="https://www.webdavsystem.com/media/1967/datalakestorageblobcontainer.png" rel="118191"></p>
<p>Confirm the new container creation.</p>
<p>Specify the container name&nbsp;in&nbsp;<code class="code">appsettings.webdav.json</code>&nbsp;as&nbsp;<code class="code">DataLakeContainerName</code>&nbsp;setting value.</p>
</li>
</ol>
<h2>Granting the App Permissions in Azure Data Lake</h2>
<p>In this section we will&nbsp;register the application in Azure AD and grant it permissions in Azure Data Lake.</p>
<ol>
<li>
<p>Navigate to the <em>Overview</em> in the Azure AD directory:</p>
<p><img id="__mcenew" alt="Copy the Primary domain field and paste it into the Domain field in appsettings.webdav.json" src="https://www.webdavsystem.com/media/2058/19azureadprimarydomain.png" rel="120366"></p>
<p>Copy the <em>Primary domain</em> field and paste it into the&nbsp;<code class="code">Domain</code> field in <code class="code">appsettings.webdav.json</code>.</p>
</li>
<li>
<p>Navigate to <em>Azure Active Directory</em> -&gt; <em>App Registrations</em>. Select <em>New Registration</em>.</p>
<p><img id="__mcenew" alt="New application registration in Azure AD" src="https://www.webdavsystem.com/media/2049/9azureadappregistrationnew.png" rel="120357"></p>
</li>
<li>
<p>Enter the app name.&nbsp;You MUST also enter the <em>Redirect URI</em>. Confirm registration.</p>
<p><img id="__mcenew" alt="Redirect URI is required. Path must match the setting in CallbackPath setting in appsettings.webdav.json" src="https://www.webdavsystem.com/media/2051/10azureadappregistrationnewdetails.png" rel="120361"></p>
<p>Note that in your project in&nbsp;<code class="code">appsettings.webdav.json</code> you already have<span>&nbsp;</span><code class="code"><span class="pl-s"><span class="pl-pds">"</code>CallbackPath<span class="pl-pds">"</span></span>: <span class="pl-s"><span class="pl-pds">"</span>/signin-oidc<span class="pl-pds">"</span></span></span>&nbsp;setting specified. The path in the <em>Redirect URI</em> must match, but you should NOT change it to a full URI in setting.</p>
</li>
<li>
<p>Open the newly created app registration.</p>
<p><img id="__mcenew" alt="Copy the Application (client) ID and Directory (tenant) ID into appsettings.webdav.json." src="https://www.webdavsystem.com/media/2059/11azureadclientidtenantid1.png" rel="120368"></p>
<p>Copy the <em>Application (client) ID</em> and <em>Directory (tenant) ID</em> fieds and paste them into <code class="code">ClientId</code> and&nbsp;<span><code class="code">TenantId</code> settings</span>&nbsp;in&nbsp;<code class="code">appsettings.webdav.json</code>.</p>
</li>
<li>
<p>Navigate to <em>Certificates &amp; secrets</em>. Select <em>New client secret</em>. Enter the secret name and confirm client secret creation.</p>
<p><img id="__mcenew" alt="Create new client secret" src="https://www.webdavsystem.com/media/2050/12azureadnewclientsecret.png" rel="120359"></p>
<p>Copy the newly created secret value and past it into <code class="code">ClientSecret</code> setting in&nbsp;in&nbsp;<code class="code">appsettings.webdav.json</code></p>
<p><img id="__mcenew" alt="Copy the newly created secret value and past it into ClientSecret setting in in appsettings.webdav.json" src="https://www.webdavsystem.com/media/2052/14azureadcopyappsecret.png" rel="120360"></p>
</li>
<li>
<p>Authorize the application to call Data Lake API. Go to <em>API Permissions</em> in the new application. Select <em>Add a Permission</em>.</p>
<p><img id="__mcenew" alt="In API Permissions select Add a Permission." src="https://www.webdavsystem.com/media/2054/13azureadaddapppermission.png" rel="120358"></p>
<p>Select <em>Azure Data Lake</em>.</p>
<p><img id="__mcenew" alt="Select Azure Data Lake" src="https://www.webdavsystem.com/media/2056/15azureadapipermissions.png" rel="120362"></p>
<p>Check <em>user_impersonation</em>. Confirm adding permission.</p>
<p><img id="__mcenew" alt="Check user_impersonation" src="https://www.webdavsystem.com/media/2055/16azureadapiimpersonationpermission.png" rel="120363"></p>
</li>
<li>
<p>Select <em>Grant admin consent</em>:</p>
<p><img id="__mcenew" alt="Grant admin concent" src="https://www.webdavsystem.com/media/2057/17azureadapigrantadminconcent.png" rel="120365"></p>
<p>&nbsp;</p>
</li>
</ol>
<h2>Granting Permissions to Azure AD Principal in Azure Data Lake</h2>
<p>In this section, we will create a new Azure AD group and will grant it permission in Azure Data Lake.</p>
<h3>Granting Permissions on Azure Data Lake Root Container</h3>
<p>Every group or user that must have access to the Azure Data Lake must first be granted permissions at a root container level.</p>
<ol>
<li>
<p>Create a new Azure AD group. Go to&nbsp;<span>Azure Active Directory -&gt; Groups. Select New Group.</span></p>
<p><img id="__mcenew" alt="Go to Azure Active Directory -&gt Groups -&gt New Group" src="https://www.webdavsystem.com/media/2040/2azurenewgroup.png" rel="120347"></p>
<p>Enter a group name and confirm group creation:<span></span></p>
<p><img id="__mcenew" alt="Provide Azure AD group name" src="https://www.webdavsystem.com/media/2039/3azurewritegroup.png" rel="120348"></p>
</li>
<li>
<p>Give Azure AD group access to the root Azure Data Lake container. Note that this operation can not be done via the Azure web portal. To do this you must use the <a href="https://aka.ms/portalfx/downloadstorageexplorer">Microsoft Azure Storage Explorer</a>.</p>
<p>Select Azure Data Lake container and select Manage Access in the context menu:&nbsp;</p>
<p><img id="__mcenew" alt="Select Azure Data Lake container and select Manage Access in context menu" src="https://www.webdavsystem.com/media/2041/4-0azurestorageexplorermanageaccess.png" rel="120346"></p>
<p>Select "Add":</p>
<p><img id="__mcenew" alt="Select Azure AD group to add to Azure Data Lake container" src="https://www.webdavsystem.com/media/2042/4-2azurestorageaddgroup.png" rel="120349"></p>
<p>Select group to add:</p>
<p><img id="__mcenew" alt="Select Azure AD group to add" src="https://www.webdavsystem.com/media/2044/4-3azurestorageaddgroup.png" rel="120351"></p>
</li>
<li>
<p>Grant permissions in Azure Data Lake on the root container. To grant permissions to read files and browse select "Read". To grant permissions to modify files, upload files, create folders, and delete files and folders select "Write". For the user to be able to list the content of the container or folder select "Execute". To make sure newly created folders in Azure Data Lake copy ("inherit") permissions from the parent object select the "Default"&nbsp;checkbox:</p>
<p><img id="__mcenew" alt="Grant permissions in Azure Data Lake on the root container." src="https://www.webdavsystem.com/media/2043/4-4azurestorageaddgroup.png" rel="120350"></p>
</li>
</ol>
<h3>Granting Permissions at a Azure Data Lake Folder Level.</h3>
<p>Now, when you have added permissions on the&nbsp;Azure Data Lake root container, you can create folders and files under the root container and grant permissions on them. Unlike with the root container this can be done via Azure web portal.&nbsp;Note that the group must be already added at the root container level, as described above, via Microsoft Azure Storage Explorer.</p>
<ol>
<li>
<p>Find the Azure AD user or group that you wish to grant permissions and copy it's Object Id:</p>
<p><img id="__mcenew" alt="Copy Azure AD Group Object Id" src="https://www.webdavsystem.com/media/2046/6azuregroupobjectid.png" rel="120353"></p>
</li>
<li>
<p>Go to Storage Explorer. Select the folder or file on which you will grant permissions. Select Manage Access. Enter the copied Object Id to 'Add user, group or service principal' field. Select Add:</p>
<p><img id="__mcenew" alt="Enter the copied Object Id into Manage Access dialog on a folder in Storage Explorer" src="https://www.webdavsystem.com/media/2048/7azureaddgrouptodatalakefolder1.png" rel="120355"></p>
</li>
<li>
<p>Select permissions that you wish to grant. here you will typically select "Default" for the permissions to be copied to the subfolders and files. Select Save:</p>
<p><img id="__mcenew" alt="Select permissions that you wish to grant to a folder or file to Azure AD user or group" src="https://www.webdavsystem.com/media/2047/8aazuregroupdatalakefoldepermissionsr.png" rel="120354"></p>
</li>
</ol>
<h2><span>Publishing the Project to Azure</span></h2>
<p><span class="warn">Note that this sample provides optimal performance when deployed directly to Microsoft Azure.</span></p>
<p>If you are using Visual Studio you can publish the project to Azure from the Visual Studio project context menu:</p>
<p><img id="__mcenew" alt="In Visual Studio you can publish the project to Azure from Visual Studio project context menu." src="https://www.webdavsystem.com/media/1968/webdavserverpublishtoazure.png" rel="118193"></p>
<p>To publish as an Azure App Service select the App Service option:</p>
<p><img id="__mcenew" alt="Creating Azure App Service" src="https://www.webdavsystem.com/media/1971/azureappservicepublish.png" rel="118203"></p>
<p>Note that you do NOT need to create a storage account in this wizard as you have created and configured it on previous steps.</p>
<h3>Enabling Web Sockets</h3>
<p>This project is using web sockets to update the user interface when any data is modified on the server-side. If you deploy your project as an App Service, by default web sockets will be disabled on the newly created app. To enable web sockets go to App Service&nbsp;<em>Configuration</em> and enable them on&nbsp;the <em>General Settings</em> tab:</p>
<p><img id="__mcenew" alt="Enable web sockets in the Azure App Service Configuration on the General Settings tab" src="https://www.webdavsystem.com/media/1970/webdavserverazurewebsockets.png" rel="118196"></p>
<p>Now you are ready to run the application. Open a web browser and navigate to https://&lt;yourservicename&gt;.azurewebsites.net:</p>
<p><img id="__mcenew" alt="WebDAV Server running on Azure as App Service" src="https://www.webdavsystem.com/media/1972/azureblobwebdavserver.png" rel="118204"></p>
<p>&nbsp;</p>
<h3>See also:&nbsp;</h3>
<ul>
<li><a title="Search" href="https://www.webdavsystem.com/server/server_examples/azure_blob_data_lake/cognitive_search/">Full-Text Search Configuration in Azure Data Lake using Cognitive Search</a></li>
</ul>
<h3 class="para d-inline next-article-heading">Next Article:</h3>
<a title="CalDAV Server with SQL Back-end Example" href="https://www.webdavsystem.com/server/server_examples/caldav_sql/">ASP.NET CalDAV Server Example with Microsoft SQL Back-end, C#</a>

