

<%@ Page Async="true" Title="WebDAV" Language="C#" AutoEventWireup="true" Inherits="WebDAVServer.FileSystemStorage.AspNet.MyCustomHandlerPage" %>

<%@ Import Namespace="ITHit.WebDAV.Server.Class1" %>
<!DOCTYPE html>
<html lang="en">
<head>
    <title>IT Hit WebDAV Server Engine</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=0, minimum-scale=1.0, maximum-scale=1.0">
    <!--
    JavaScript file required to run WebDAV Ajax library is loaded from Node.js Package Manager.
    To load files from your website download them here: https://www.webdavsystem.com/ajax/download,
    deploy them to your website and replace the path below in this file.
    -->
    <script type="module" crossorigin src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/app.js"></script>
    <link rel="stylesheet" crossorigin href="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/app.css" />
    <link rel="icon" href="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/favicon.ico" />
</head>
<body>
    <div id="app"></div>
    <script>
        var webDavSettings = {
            // IT Hit WebDAV Ajax Library activation info:
            // https://www.webdavsystem.com/ajax/programming/activating/
            // - The trial version is fully functional and does not have any limitations, however it is time-limited and will stop working when the trial period expires.
            // - In case you have a perpetual license, replace the JavaScript file(s) with the file(s) from the non-trial version. Download the non-trial version in the product download area.
            // - In case of a subscription license set the License ID in JavaScript. Get the License ID in the product download area:
            //LicenseId: 'XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX',
            WebDavServerPath: '<%=Request.ApplicationPath.TrimEnd('/')%>',
            ApplicationProtocolsPath: '<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/webdav.client/Plugins/',
			EditDocAuth: {
                Authentication: 'anonymous',                       // Authentication to use when opening documents for editing: 'anonymous', 'challenge', 'ms-ofba', 'cookies'
                CookieNames: null,                                 // Coma separated list of cookie names to search for.
                SearchIn: null,                                    // Web browsers to search and copy permanent cookies from: 'current', 'none'.
                LoginUrl: null                                     // Login URL to redirect to in case any cookies specified in CookieNames parameter are not found.
            },
            WebDavServerVersion: 'v<%=System.Reflection.Assembly.GetAssembly(typeof(ITHit.WebDAV.Server.DavEngineAsync)).GetName().Version%>'
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
</body>
</html>
