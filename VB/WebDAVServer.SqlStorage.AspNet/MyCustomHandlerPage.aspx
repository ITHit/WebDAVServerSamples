

<%@ Page Async="true" Title="WebDAV" Language="C#" AutoEventWireup="true" Inherits="WebDAVServer.SqlStorage.AspNet.MyCustomHandlerPage" %>

<%@ Import Namespace="ITHit.WebDAV.Server.Class1" %>
<!DOCTYPE html>
<html lang="en">
<head>
    <title>IT Hit WebDAV Server Engine</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=0, minimum-scale=1.0, maximum-scale=1.0">
    <link href="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/app.css" rel="stylesheet" />
</head>
<body>
    <header>
        <div class="navbar navbar-dark bg-dark shadow-sm">
            <div class="container-fluid d-flex justify-content-between" data-toggle="collapse" data-target="#navbarHeader" aria-controls="navbarHeader" aria-expanded="false" aria-label="Toggle navigation">
                <span class="navbar-brand d-flex align-items-center ellipsis">
                    <img src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/images/logo.svg" alt="IT Hit logo" class="logo" />
                    <span>IT Hit WebDAV Server Engine v<%=System.Reflection.Assembly.GetAssembly(typeof(ITHit.WebDAV.Server.DavEngineAsync)).GetName().Version%></span>
                </span>
                <button class="navbar-toggler burger-button collapsed js-collapse" type="button" data-toggle="collapse" data-target="#navbarHeader" aria-controls="navbarHeader" aria-expanded="false" aria-label="Toggle navigation">
                    <div class="burger-icon"><span></span><span></span><span></span></div>
                </button>
            </div>
        </div>
        <div class="collapse bg-dark" id="navbarHeader">
            <div class="header-content">
                <div class="container-fluid justify-content-between">
                    <div class="row">
                        <div class="col">
                            <p>
                                This page is displayed when user accesses any folder on your WebDAV server in a web browser.
                                You can customize this page to your needs.
                            </p>
                        </div>
                    </div>
                    <div class="row blocks">
                        <div class="col-12 col-lg-4 d-flex flex-column">
                            <h3>Test Your Server</h3>
                            <p>
                                To test your WebDAV server you can run Ajax integration tests right from this page.
                            </p>
                            <a href="javascript:void(0)" onclick="OpenTestsWindow()" class="align-self-start btn btn-primary" role="button">Run Integration Tests</a>
                        </div>
                        <div class="col-12 col-lg-4 d-flex flex-column">
                            <h3>Manage Docs with Ajax File Browser</h3>
                            <p>
                                Use the <a href="https://www.webdavsystem.com/ajaxfilebrowser/programming/">IT Hit Ajax File Browser</a> to browse your documents, open for editing from a web page and
-                              uploading with pause/resume and auto-restore upload.
                            </p>
                            <a href="javascript:void(0)" onclick="OpenAjaxFileBrowserWindow()" class="align-self-start btn btn-primary" role="button">Browse Using Ajax File Browser</a>
                        </div>
                        <div class="col-12 col-lg-4 d-flex flex-column">
                            <h3>Connect with WebDAV Client</h3>
                            <p>
                                Use a WebDAV client provided with almost any OS. Refer to <a href="https://www.webdavsystem.com/server/access">Accessing WebDAV Server</a> page for
                -              detailed instructions. The button below is using <a href="https://www.webdavsystem.com/ajax/">IT Hit WebDAV Ajax Library</a> to mount WebDAV
                -              folder and open the default OS file manager.
                            </p>
                            <a href="javascript:void(0)" onclick="WebDAVController && WebDAVController.OpenCurrentFolderInOsFileManager()" class="align-self-start btn btn-primary" role="button">Browse Using OS File Manager</a>
                        </div>
                    </div>
                    <div class="row mt-1">
                        <div class="col">
                            <p class="versions">
                                IT Hit WebDAV Server Engine for .NET: v<%=System.Reflection.Assembly.GetAssembly(typeof(ITHit.WebDAV.Server.DavEngineAsync)).GetName().Version%>
                            </p>
                            <p class="versions">
                                IT Hit WebDAV AJAX Library: <span class="ithit-version-value"></span>
                            </p>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </header>
    <main role="main" class="container-fluid">
        <div id="app" data-template="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/app.html"></div>
    </main>
    <script>
        var webDavSettings = {
            WebDavServerPath: '<%=Request.ApplicationPath.TrimEnd('/')%>',
            ApplicationProtocolsPath: '<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/webdav.client/Plugins/',
			EditDocAuth: {
                Authentication: 'anonymous',                       // Authentication to use when opening documents for editing: 'anonymous', 'challenge', 'ms-ofba', 'cookies'
                CookieNames: null,                                 // Coma separated list of cookie names to search for.
                SearchIn: null,                                    // Web browsers to search and copy permanent cookies from: 'current', 'none'.
                LoginUrl: null                                     // Login URL to redirect to in case any cookies specified in CookieNames parameter are not found.
            }
        }

        function OpenAjaxFileBrowserWindow() {
            window.open("<%=Request.ApplicationPath.TrimEnd('/')%>/AjaxFileBrowser/AjaxFileBrowser.aspx", "", "menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=900,height=600");
        }

        function OpenTestsWindow() {
            var width = Math.round(screen.width * 0.5);
            var height = Math.round(screen.height * 0.8);
            window.open("<%=Request.ApplicationPath.TrimEnd('/')%>/AjaxFileBrowser/AjaxIntegrationTests.aspx", "", "menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=" + width + ",height=" + height);
        }
    </script>
    <!--
    JavaScript file required to run WebDAV Ajax library is loaded from Node.js Package Manager.
    To load files from your website download them here: https://www.webdavsystem.com/ajax/download,
    deploy them to your website and replace the path below in this file.
    -->
    <script src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/app.js"></script>
</body>
</html>
