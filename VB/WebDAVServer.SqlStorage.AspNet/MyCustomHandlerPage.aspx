
<%@ Page Async="true" Title="WebDAV" Language="C#" AutoEventWireup="true" Inherits="WebDAVServer.SqlStorage.AspNet.MyCustomHandlerPage" %>

<%@ Import Namespace="ITHit.WebDAV.Server.Class1" %>
<!DOCTYPE html>
<html lang="en">
<head>
    <title>IT Hit WebDAV Server Engine</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=0, minimum-scale=1.0, maximum-scale=1.0">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/4.0.0/css/bootstrap.min.css" rel="stylesheet">
    <link href="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/css/webdav-layout.css" rel="stylesheet"/>  
</head>
<body>
    <header>
        <div class="navbar navbar-dark bg-dark shadow-sm">
            <div class="container-fluid d-flex justify-content-between">
                <a href="/" class="navbar-brand d-flex align-items-center ellipsis">
                    <img src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/images/logo.svg" alt="IT Hit logo" class="logo"/>
                     <span>IT Hit WebDAV Server Engine v<%=System.Reflection.Assembly.GetAssembly(typeof(ITHit.WebDAV.Server.DavEngineAsync)).GetName().Version%></span>        
                </a>
                <button class="navbar-toggler burger-button collapsed" type="button" data-toggle="collapse" data-target="#navbarHeader" aria-controls="navbarHeader" aria-expanded="false" aria-label="Toggle navigation">
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
                        <div class="col-12 col-lg-4">
                            <h3>Your Test Folder</h3>
                            <p>
                               To test your WebDAV server you can run Ajax integration tests right from this page.
                            </p>
                            <a href="javascript:void(0)" onclick="OpenTestsWindow()" class="btn btn-primary" role="button">Run Integration Tests</a>
                        </div>
                        <div class="col-12 col-lg-4">
                            <h3>Manage Docs with Ajax File Browser</h3>
                            <p>
                               Use the <a href="https://www.webdavsystem.com/ajaxfilebrowser/programming/">IT Hit Ajax File Browser</a> to browse your documents, open for editing from a web page and
-                              uploading with pause/resume and auto-restore upload.
                            </p>
                            <a href="javascript:void(0)" onclick="OpenAjaxFileBrowserWindow()" class="btn btn-primary" role="button">Browse Using Ajax File Browser</a>
                        </div>
                        <div class="col-12 col-lg-4">
                            <h3>Connect with WebDAV Client</h3>
                            <p>                               
                               Use a WebDAV client provided with almost any OS. Refer to <a href="https://www.webdavsystem.com/server/access">Accessing WebDAV Server</a> page for
                -              detailed instructions. The button below is using <a href="https://www.webdavsystem.com/ajax/">IT Hit WebDAV Ajax Library</a> to mount WebDAV
                -              folder and open the default OS file manager.
                            </p>
                            <a href="javascript:void(0)" onclick="WebDAVController && WebDAVController.OpenCurrentFolderInOsFileManager()" class="btn btn-primary" role="button">Browse Using OS File Manager</a>
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
         <div class="row no-gutters">
            <div id="leftPanel" class="col">
                <nav aria-label="breadcrumb">
                    <ol class="breadcrumb ithit-breadcrumb-container"></ol>
                </nav>
                <div class="ithit-search-container">
                    <input class="form-control" type="text" />
                </div>
                <div class="ithit-grid-toolbar row">
                    <div class="col">
                        <button class="btn-create-folder"><span class="d-none d-lg-inline">Create Folder</span></button>
                    </div>
                    <div class="col-auto px-0 px-md-3">
                        <button class="btn-copy-items" disabled="disabled"></button>
                        <button class="btn-cut-items" disabled="disabled"></button>
                        <button class="btn-paste-items" disabled="disabled"></button>
                        <button class="btn-reload-items"></button>
                    </div>
                    <div class="col-auto px-1 px-lg-3">
                        <button class="btn-print-items" disabled="disabled"><span class="d-none d-lg-inline">Print</span></button>
                        <button class="btn-delete-items" disabled="disabled"><span class="d-none d-lg-inline">Delete</span></button>
                    </div>
                </div>
                <div class="table-responsive">
                    <table class="table table-hover ithit-grid-container">
                        <thead>
                            <tr>
                                <th scope="col"><label class="custom-checkbox"><input type="checkbox" /><span class="checkmark"></span></label></th>
                                <th scope="col"></th>
                                <th class="ellipsis sort" scope="col" data-sort-column="displayname"><span>Display Name</span></th>
                                <th class="d-none d-xl-table-cell sort" scope="col" data-sort-column="getcontenttype">Type</th>
                                <th class="sort" scope="col" data-sort-column="quota-used-bytes">Size</th>
                                <th class="d-none d-lg-table-cell sort" scope="col" data-sort-column="getlastmodified">Modified</th>
                                <th class="column-action" scope="col"></th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
                <div class="row">
                    <div class="col-12 align-items-end">
                        <nav aria-label="Page navigation">
                            <ul class="pagination flex-wrap justify-content-end ithit-pagination-container"></ul>
                        </nav>
                    </div>
                </div>
                <div id="ithit-dropzone" class="">
                    <div class="text-center boxtitle">
                        Drop files or folders to upload
                    </div>
                </div>
                <input id="ithit-hidden-input" class="d-none" type="file" multiple>
                <div class="table-responsive">
                    <table class="table ithit-grid-uploads d-none">
                        <thead>
                            <tr>
                                <th class="ellipsis" scope="col"><span>Display Name</span></th>
                                <th class="d-none d-sm-table-cell text-right" scope="col">Size</th>
                                <th class="d-none d-sm-table-cell text-right" scope="col">Uploaded</th>
                                <th class="d-none d-sm-table-cell text-right" scope="col">%</th>
                                <th class="d-none d-md-table-cell d-lg-block custom-hidden text-right" scope="col">Elapsed</th>
                                <th class="text-right" scope="col">Remaining</th>
                                <th class="d-none d-md-table-cell text-right" scope="col">Speed</th>
                                <th class="d-none d-md-table-cell custom-hidden" scope="col">State</th>
                                <th></th>
                            </tr>
                        </thead>
                        <tbody></tbody>
                    </table>
                </div>
                <div id="ConfirmModal" class="modal" tabindex="-1" role="dialog" aria-labelledby="ConfirmModalLabel">
                    <div class="modal-dialog modal-sm" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">Confirm</h5>
                                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                    <span aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <p class="message"></p>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-primary btn-ok">OK</button>
                                <button type="button" class="btn btn-default" data-dismiss="modal">Cancel</button>
                            </div>
                        </div>
                    </div>
                </div>
                <div id="CreateFolderModal" class="modal" tabindex="-1" role="dialog" aria-labelledby="CreateFolderModalLabel">
                    <div class="modal-dialog" role="document">
                        <div class="modal-content">
                            <form action="/" method="post">
                                <div class="modal-header">
                                    <h5 class="modal-title">Create Folder</h5>
                                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                        <span aria-hidden="true">&times;</span>
                                    </button>
                                </div>
                                <div class="modal-body">
                                    <div class="form-group">
                                        <input type="text" class="form-control" id="NameFolder" placeholder="Folder Name" />
                                        <div class="alert alert-danger d-none">
                                        </div>
                                    </div>
                                </div>
                                <div class="modal-footer">
                                    <button type="submit" class="btn btn-primary btn-submit">OK</button>
                                    <button type="button" class="btn btn-default" data-dismiss="modal">Cancel</button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
                <div id="ConfirmRewriteModal" class="modal" tabindex="-1" role="dialog" aria-labelledby="ConfirmRewriteModalLabel">
                    <div class="modal-dialog modal-sm" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">Confirm</h5>
                                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                    <span aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <p class="message"></p>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-primary btn-ok">Yes</button>
                                <button type="button" class="btn btn-no">No</button>
                                <button type="button" class="btn btn-default" data-dismiss="modal">Cancel</button>
                            </div>
                        </div>
                    </div>
                </div>
                <div id="ErrorModal" class="modal" tabindex="-1" role="dialog" aria-labelledby="ConfirmModalLabel">
                    <div class="modal-dialog modal-md" role="document">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title">Error</h5>
                                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                                    <span aria-hidden="true">&times;</span>
                                </button>
                            </div>
                            <div class="modal-body">
                                <div class="container-fluid">
                                    <div class="row">
                                        <div class="col-md-4"><p>Error message:</p></div>
                                        <div class="col-md-8"><p class="error-message"></p></div>
                                    </div>
                                    <div class="row error-details-row">
                                        <div class="col-md-12">
                                            <p>
                                                <button class="btn btn-default" type="button" data-toggle="collapse" data-target="#error-details-collapse" aria-expanded="false" aria-controls="error-details-collapse">
                                                    Error Details
                                                </button>
                                            </p>
                                            <div class="collapse" id="error-details-collapse">
                                                <div class="card card-body">
                                                    <div class="row">
                                                        <div class="col-md-2"><p>Url:</p></div>
                                                        <div class="col-md-10"><p class="error-details-url"></p></div>
                                                    </div>
                                                    <div class="row">
                                                        <div class="col-md-4 "><p>Error message:</p></div>
                                                        <div class="col-md-8"><p class="error-details-message"></p></div>
                                                    </div>
                                                    <div class="row error-details-row">
                                                        <div class="col-md-12">
                                                            <iframe style="width: 100%; border: none"></iframe>
                                                        </div>
                                                    </div>

                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-default" data-dismiss="modal">Close</button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div id="rightPanel" class="col">
                <div class="gsuite-container">
                    <span id="fileName" class="file-name d-none d-lg-inline"></span>
                    <ul class="nav nav-tabs" id="gSuiteTabs" role="tablist">
                        <li class="nav-item">
                            <a class="nav-link active" id="preview-tab" data-toggle="tab" href="#preview" role="tab" aria-controls="preview" aria-selected="true">Preview</a>
                        </li>
                        <li class="nav-item">
                            <a class="nav-link" id="edit-tab" data-toggle="tab" href="#edit" role="tab" aria-controls="edit" aria-selected="false">Edit</a>
                        </li>
                    </ul>
                    <div class="tab-content" id="gSuiteContainer">
                        <div class="tab-pane fade show active" id="preview" role="tabpanel" aria-labelledby="preview-tab">
                            <div id="gSuitePreview">
                            </div>
                            <div id="gSuitePreviewBackground" class="background">
                                Select a document to preview.
                            </div>
                        </div>
                        <div class="tab-pane fade" id="edit" role="tabpanel" aria-labelledby="edit-tab">
                            <div id="gSuiteEdit">
                            </div>
                            <div id="gSuiteEditBackground" class="background">
                                Select a document to edit.
                            </div>
                        </div>
                    </div>
                </div>
            </div>            
        </div>        
    </main>
    <script>
        var webDavSettings = {
            ApplicationPath: '<%=Request.ApplicationPath.TrimEnd('/')%>',
            ApplicationProtocolsPath: '<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/js/node_modules/webdav.client/Plugins/',
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
    <script src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/js/node_modules/webdav.client/ITHitWebDAVClient.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/split.js/1.3.5/split.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/typeahead.js/0.11.1/typeahead.jquery.min.js"></script>  
    <script src="https://cdnjs.cloudflare.com/ajax/libs/popper.js/1.12.9/umd/popper.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/4.0.0/js/bootstrap.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/moment.js/2.24.0/moment.min.js"></script>    
    <script src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/js/webdav-common.js"></script>
    <script src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/js/webdav-gridview.js"></script>
    <script src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/js/webdav-uploader.js"></script>
    <script src="<%=Request.ApplicationPath.TrimEnd('/')%>/wwwroot/js/webdav-websocket.js"></script>
</body>
</html>
