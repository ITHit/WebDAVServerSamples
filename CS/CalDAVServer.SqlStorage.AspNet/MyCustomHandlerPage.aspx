

<%@ Page Async="true" Title="WebDAV" Language="C#" AutoEventWireup="true" Inherits="CalDAVServer.SqlStorage.AspNet.MyCustomHandlerPage" %>

<%@ Import Namespace="CalDAVServer.SqlStorage.AspNet" %>
<!DOCTYPE html>
<html lang="en">
<head>
    <title>IT Hit CalDAV / CardDAV Server</title>
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=0, minimum-scale=1.0, maximum-scale=1.0" />
    <link href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/3.3.7/css/bootstrap.min.css" rel="stylesheet" />
    <style>
        body {
            padding-bottom: 40px;
        }

        .navbar-header h1 {
            height: auto;
            padding: 15px;
            margin: 0;
            font-weight: normal;
            font-size: 18px;
            line-height: 20px;
            color: #9d9d9d;
        }

        p {
            word-break: break-all;
        }

        .btn-default {
            white-space: initial;
        }

        .table-responsive {
            border: none;
        }
    </style>
</head>
<body>
    <nav class="navbar navbar-inverse navbar-static-top">
        <div class="container-fluid">
            <div class="navbar-header">
                <h1>IT Hit WebDAV Server Engine v<%=System.Reflection.Assembly.GetAssembly(typeof(ITHit.WebDAV.Server.DavEngineAsync)).GetName().Version %></h1>
            </div>
        </div>
    </nav>
    <div class="container-fluid">
        <div class="row">
            <div class="col-md-8">
                <h3>Calendars:</h3>
                <div class="table-responsive">
                    <table class="table table-hover ithit-grid-container">
                        <colgroup>
                            <col width="32" />
                            <col />
                            <col />
                            <col />
                        </colgroup>
                        <thead>
                            <tr>
                                <th></th>
                                <th>Name</th>
                                <th>Url</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody>
                            <% foreach (ITHit.WebDAV.Server.IItemCollectionAsync item in AllUserCalendars) { %><tr><td class="text-center"><span class="glyphicon glyphicon-calendar"></span></td><td><%=item.Name %></td><td><%=ApplicationPath.TrimEnd(new char[]{'/'})+"/"+item.Path.TrimStart(new char[]{'/'}) %></td><td><a href="<%=ApplicationPath.TrimEnd(new char[]{'/'})+"/"+item.Path.TrimStart(new char[]{'/'}) %>?connect" class="btn btn-default">Connect</a></td></tr><% } %>
                        </tbody>
                    </table>
                </div>
                <hr />
            </div>
            <div class="col-md-4">
                <p>This page is displayed when user accesses any folder on your CalDAV / CardDAV server in a web browser. You can customize this page to your needs.</p>
                <p>To find how to configure email notifications and setup server on IIS please follow this <a href="http://www.webdavsystem.com/server/server_examples/caldav_carddav_csharp">link</a></p>
                <hr />
                <h3>Connect with CalDAV / CardDAV Client</h3>
                <p>Follow this links to find how to <a href="http://www.webdavsystem.com/server/access/caldav/">sync calendars</a> and <a href="http://www.webdavsystem.com/server/access/carddav/">address books</a> with your CalDAV / CardDAV client.</p>
                <p>
                    Most CalDAV and CardDAV clients support discovery, so you can use the short URL to connect:<br />
                    <b><%=ApplicationPath %></b>
                </p>
                <hr />
                <h3>Examine Your Server Content</h3>
                <p>
                    Examine your CalDAV / CardDAV server content with IT Hit Ajax File Browser. It will give you an idea of how clendars, events, address books and contacts are organized on your server.
                </p>
                <a href="javascript:OpenAjaxFileBrowserWindow()" class="btn btn-default">Open in Ajax File Browser</a>
            </div>            
        </div>
    </div>
    <script type="text/javascript">
        var port = window.location.port;
        if (port == "")
            port = window.location.protocol == 'http:' ? '80' : '443';
        var webDavFolderUrl = window.location.protocol + '//' + window.location.hostname + ':' + port + '/';

        function OpenAjaxFileBrowserWindow() {
            window.open("/AjaxFileBrowser/AjaxFileBrowser.aspx", "", "menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=900,height=600");
        }
    </script>
</body>
</html>

