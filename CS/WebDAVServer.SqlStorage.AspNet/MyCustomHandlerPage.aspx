

<%@ Page Async="true" Title="WebDAV" Language="C#" AutoEventWireup="true" Inherits="WebDAVServer.SqlStorage.AspNet.MyCustomHandlerPage" %>

<%@ Import Namespace="ITHit.WebDAV.Server.Class1" %>
<!DOCTYPE html>
<html lang="en">
<head>
    <title>IT Hit WebDAV Server Engine</title>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">
    <meta name="viewport" content="width=device-width, initial-scale=1.0, user-scalable=0, minimum-scale=1.0, maximum-scale=1.0">
    <link href="https://cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/3.3.7/css/bootstrap.min.css" rel="stylesheet">
    <style>
        .navbar-header h1 {
            height: auto;
            padding: 15px;
            margin: 0;
            font-weight: normal;
            font-size: 18px;
            line-height: 20px;
            color: #9d9d9d;
        }

        .ithit-search-container {
            position: relative;
            height: 50px;
        }

            .ithit-search-container input.tt-input[disabled],
            .ithit-search-container input.tt-input[readonly] {
                cursor: default;
            }

            .ithit-search-container .twitter-typeahead {
                position: relative;
                width: 100%;
                padding: 0 100px 0 0;
                margin-bottom: 15px;
            }

            .ithit-search-container button {
                position: absolute;
                top: 0;
                right: 0;
                width: 85px;
                height: 34px;
            }

        @media (max-width: 767px) {
            .ithit-search-container .twitter-typeahead {
                padding-right: 53px;
            }

            .ithit-search-container button {
                width: 38px;
            }
        }

        .ithit-breadcrumb-container {
            height: 36px;
        }

            .ithit-breadcrumb-container .glyphicon {
                width: 18px;
            }

        .ithit-grid-container {
            width: 100%;
        }

            .ithit-grid-container .column-action {
                width: 180px;
            }

            .ithit-grid-container .column-action a {
                display: inline-block;
                margin-right: 10px;
            }

            .ithit-grid-container .column-action a:last-child {
                margin-right: 0;
            }

            .ithit-grid-icon-locked {
                color: #337ab7;
                left: 2px;
                top: -1px;
            }

            .glyphicon .ithit-grid-icon-locked {
                left: -4px;
                top: 3px;
                text-shadow: -1px -1px 0px #fff;
                font-size: 12px;
            }

        @media (max-width: 767px) {
            .ithit-grid-container .column-action {
                width: 14px;
                padding: 8px 8px 8px 0;
                text-align: right;
            }
        }

        .tt-suggestion .snippet, .ithit-grid-container .snippet {
            height: 30px;
            overflow: hidden;
            font-size: 12px;
            line-height: 18px;
            color: #999;
        }

        .tt-suggestion .breadcrumb, .ithit-grid-container .breadcrumb {
            font-size: 12px;
            color: #999;
            word-break: break-word;
        }

        .ithit-grid-container ul.breadcrumb, .tt-suggestion ul.breadcrumb {
            list-style: none;
            background-color: transparent;
            padding: 0 0 0 8px;
            margin: 0;
        }

        .tt-suggestion ul.breadcrumb {
            padding: 0;
        }

            .tt-suggestion ul.breadcrumb li:first-child, .ithit-grid-container ul.breadcrumb li:first-child {
                display: none;
            }

            .ithit-grid-container ul.breadcrumb li, .tt-suggestion ul.breadcrumb li {
                display: inline-block;
                margin-right: 5px;
            }

                .ithit-grid-container ul.breadcrumb li .glyphicon, .ithit-grid-container ul.breadcrumb li:nth-of-type(2):before,
                .tt-suggestion ul.breadcrumb li .glyphicon, .tt-suggestion ul.breadcrumb li:nth-of-type(2):before {
                    display: none;
                }

                .ithit-grid-container ul.breadcrumb li:before, .tt-suggestion ul.breadcrumb li:before {
                    padding: 0;
                }

        .tt-suggestion .snippet b, .ithit-grid-container .snippet b {
            color: #555;
        }

        .tt-hint {
            color: #999;
        }

        .tt-menu {
            width: 100%;
            right: 100px;
            margin: 1px 0;
            padding: 6px 0;
            background-color: #fff;
            border: 1px solid #ccc;
            -webkit-border-radius: 4px;
            border-radius: 4px;
            -webkit-box-shadow: 0 6px 12px rgba(0, 0, 0, .175);
            box-shadow: 0 6px 12px rgba(0, 0, 0, .175);
        }

        .tt-suggestion {
            padding: 3px 20px;
            line-height: 1.7;
        }

            .tt-suggestion:hover {
                cursor: pointer;
                background-color: #eee;
            }

            .tt-suggestion.tt-cursor {
                background-color: #eee;
            }

        table tr.tr-snippet-url td {
            padding: 0px;
            border-top: none;
        }

            table tr.tr-snippet-url td > div {
                padding-left: 8px;
            }

                table tr.tr-snippet-url td > div:last-child {
                    margin-bottom: 8px;
                    padding-right: 8px;
                }

        table tr.hover {
            background-color: #f5f5f5;
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
                <h1>
                    IT Hit WebDAV Server Engine v<%=System.Reflection.Assembly.GetAssembly(typeof(ITHit.WebDAV.Server.DavEngineAsync)).GetName().Version%>
                </h1>
            </div>
        </div>
    </nav>

    <div class="container-fluid">
        <div class="row">
            <div class="col-md-8">
                <ul class="breadcrumb ithit-breadcrumb-container"></ul>

                <div class="ithit-search-container">
                    <input class="form-control" type="text" />
                    <button type="button" class="btn btn-primary">
                        <span class="glyphicon glyphicon-search visible-xs"></span>
                        <span class="hidden-xs">Search</span>
                    </button>
                </div>

                <div class="table-responsive">
                    <table class="table table-hover ithit-grid-container">
                        <colgroup>
                            <col width="40" class="hidden-xs" />
                            <col width="32" class="hidden-xs" />
                            <col />
                            <col width="100" class="hidden-xs" />
                            <col width="180" class="hidden-xs" />
                            <col width="50" class="visible-xs hidden-sm hidden-md hidden-lg" />
                            <col />
                        </colgroup>
                        <thead>
                            <tr>
                                <th class="hidden-xs">#</th>
                                <th class="hidden-xs"></th>
                                <th>Display Name</th>
                                <th>Size</th>
                                <th class="hidden-xs">Modified</th>
                                <th class="column-action"></th>
                            </tr>
                        </thead>
                        <tbody>
                        </tbody>
                    </table>
                </div>
            </div>
            <div class="col-md-4">
                <p>
                    This page is displayed when user accesses any folder on your WebDAV server in a web browser.
                You can customize this page to your needs.
                </p>

                <p>
                    Examine the MyCustomHandlerPage.html/aspx in your project to see how to <a href="https://www.webdavsystem.com/ajax/programming/" target="_blank">list folder content</a>
                    and to use IT Hit WebDAV Ajax Library to <a href="https://www.webdavsystem.com/ajax/programming/opening_ms_office_docs" target="_blank">open documents for editing</a>.
                </p>

                <hr />

                <h3>Test Your Server</h3>

                <p>
                    To test your WebDAV server you can run Ajax integration tests right from this page.
                </p>
                <a href="javascript:void(0)" onclick="OpenTestsWindow()" class="btn btn-default">Run Integration Tests</a>

                <hr />

                <h3>Manage Docs with Ajax File Browser</h3>

                <p>
                    Use the <a href="https://www.webdavsystem.com/ajaxfilebrowser/programming/">IT Hit Ajax File Browser</a> to browse your documents, open for editing from a web page and
                uploading with pause/resume and auto-restore upload.
                </p>
                <a href="javascript:void(0)" onclick="OpenAjaxFileBrowserWindow()" class="btn btn-default">Browse Using Ajax File Browser</a>

                <hr />

                <h3>Connect with WebDAV Client</h3>

                <p>
                    Use a WebDAV client provided with almost any OS. Refer to <a href="https://www.webdavsystem.com/server/access">Accessing WebDAV Server</a> page for
                detailed instructions. The button below is using <a href="https://www.webdavsystem.com/ajax/">IT Hit WebDAV Ajax Library</a> to mount WebDAV
                folder and open the default OS file manager.
                </p>
                <a href="javascript:void(0)" onclick="WebDAVController && WebDAVController.OpenCurrentFolderInOsFileManager()" class="btn btn-default">Browse Using OS File Manager</a>

                <br />
                <br />
            </div>
        </div>
    </div>

    <!--
    JavaScript file required to run WebDAV Ajax library is loaded from IT Hit website.
    To load files from your website download them here: https://www.webdavsystem.com/ajax/download,
    deploy them to your website and replace the 'https://www.ajaxbrowser.com/ITHitService/' path in this file.
-->
    <script src="https://www.ajaxbrowser.com/ITHitService/WebDAVAJAXLibrary/ITHitWebDAVClient.js"
        type="text/javascript"></script>

    <script src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.1.0/jquery.min.js"></script>
    <script src="https://cdnjs.cloudflare.com/ajax/libs/typeahead.js/0.11.1/typeahead.jquery.min.js"></script>
    <script>

        function OpenAjaxFileBrowserWindow() {
            window.open("<%=Request.ApplicationPath.TrimEnd('/')%>/AjaxFileBrowser/AjaxFileBrowser.aspx", "", "menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=900,height=600");
    }

    function OpenTestsWindow() {
        var width = Math.round(screen.width * 0.5);
        var height = Math.round(screen.height * 0.8);
        window.open("<%=Request.ApplicationPath.TrimEnd('/')%>/AjaxFileBrowser/AjaxIntegrationTests.aspx", "", "menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=" + width + ",height=" + height);
    }

     (function () {

            // Formatters

            var Formatters = {

                /**
                 *
                 * @param {number} iSize
                 * @returns {string}
                 */
                FileSize: function (iSize) {
                    if (!iSize) {
                        return '0 B';
                    }
                    var i = Math.floor(Math.log(iSize) / Math.log(1024));
                    return (iSize / Math.pow(1024, i)).toFixed(2) * 1 + ' ' + ['B', 'kB', 'MB', 'GB', 'TB'][i];
                },

                /**
                 *
                 * @param {Date} oDate
                 * @returns {string}
                 */
                Date: function (oDate) {
                    return [
                                ('0' + (oDate.getMonth() + 1)).slice(-2),
                                ('0' + oDate.getDate()).slice(-2),
                                oDate.getFullYear()
                    ].join('/') +
                            ' ' +
                            [
                                ('0' + oDate.getHours() % 12 || 12).slice(-2),
                                ('0' + oDate.getMinutes()).slice(-2),
                                ('0' + oDate.getSeconds()).slice(-2)
                            ].join(':') +
                            ' ' +
                            (oDate.getHours() > 12 ? 'PM' : 'AM')
                },

                /**
                 *
                 * @param {string} html
                 * @returns {string}
                 */
                Snippet: function (html) {
                    if (html) {
                        var safePrefix = '__b__tag' + (new Date()).getTime();
                        html = html.replace(/<b>/g, safePrefix + '_0').replace(/<\/b>/g, safePrefix + '_1');
                        html = $('<div />').text(html).text();
                        html = html.replace(new RegExp(safePrefix + '_0', 'g'), '<b>').replace(new RegExp(safePrefix + '_1', 'g'), '</b>');
                    }
                    return $('<div />').addClass('snippet').html(html);
                }

            };

            ///////////////////
            // Folder Grid View

            var FolderGridView = function (selector) {
                this.$el = $(selector);
                this.IsSearchMode = false;

                $(selector).on({
                    mouseenter: function () {
                        if ($(this).hasClass('tr-snippet-url'))
                            $(this).addClass('hover').prev().addClass('hover');
                        else
                            $(this).addClass('hover').next().addClass('hover');
                    },
                    mouseleave: function () {
                        if ($(this).hasClass('tr-snippet-url'))
                            $(this).removeClass('hover').prev().removeClass('hover');
                        else
                            $(this).removeClass('hover').next().removeClass('hover');
                    }
                }, 'tr');
            };
            FolderGridView.prototype = {

                Render: function (aItems, bIsSearchMode) {
                    this.IsSearchMode = bIsSearchMode || false;

                    // Sort by display name
                    if (''.localeCompare) {
                        aItems.sort(function (a, b) {
                            return a.DisplayName.localeCompare(b.DisplayName);
                        });
                    } else {
                        aItems.sort(function (a, b) {
                            return a.DisplayName < b.DisplayName ? -1 : (a.DisplayName > b.DisplayName ? 1 : 0);
                        });
                    }

                    // Folders at first
                    aItems = []
                            .concat(aItems.filter(function (oItem) {
                                return oItem.IsFolder()
                            }))
                            .concat(aItems.filter(function (oItem) {
                                return !oItem.IsFolder()
                            }));

                    this.$el.find('tbody').html(
                        aItems.map(function (oItem, i) {
                            var locked = oItem.ActiveLocks.length > 0 ? '<span class="ithit-grid-icon-locked glyphicon glyphicon-lock"></span>' : '';
                            /** @type {ITHit.WebDAV.Client.HierarchyItem} oItem */
                            return $('<div/>').html([$('<tr />').html([
                                $('<td class="hidden-xs" />').text(i + 1),
                                $('<td class="hidden-xs" />').html(oItem.IsFolder() ? '<span class="glyphicon glyphicon-folder-open">' + locked + '</span>' : locked),
                                this._RenderDisplayName(oItem),
                                $('<td />').text(!oItem.IsFolder() ? Formatters.FileSize(oItem.ContentLength) : '').css('text-align', 'right'),
                                $('<td class="hidden-xs" />').text(Formatters.Date(oItem.LastModified)),
                                $('<td class="column-action" />').html(this._RenderActions(oItem))
                            ]),
                                $('<tr class="tr-snippet-url"/>').html([
                                    $('<td class="hidden-xs" />'),
                                    $('<td class="hidden-xs" />'),
                                    this._RenderSnippetAndUrl(oItem)])]).children();
                        }.bind(this))
                    );
                },

                /**
                 * @param {ITHit.WebDAV.Client.HierarchyItem} oItem
                 **/
                _RenderDisplayName: function (oItem) {
                    var oElement = oItem.IsFolder() ?
                            $('<td />').html($('<a />').text(oItem.DisplayName).attr('href', oItem.Href)) :
                            $('<td />').text(oItem.DisplayName);

                    return oElement;
                },
                _RenderSnippetAndUrl: function (oItem) {
                    var oElement = $('<td colspan="10"/>');
                    // Append path on search mode
                    if (this.IsSearchMode) {
                        new BreadcrumbsView($('<ul />').addClass('breadcrumb').appendTo(oElement)).SetHierarchyItem(oItem);

                        // Append snippet to name                  
                        oElement.append(Formatters.Snippet(oItem.Properties.Find(oWebDAV.SnippetPropertyName)));
                    }

                    return oElement;
                },

                /**
                 * @param {ITHit.WebDAV.Client.HierarchyItem} oItem
                 * @returns string
                 **/
                _RenderActions: function(oItem) {
                    var actions = [];

                    if (oItem.IsFolder()) {
                        actions.push($('<a />')
                            .html('<span class="glyphicon glyphicon-hdd" title="Browse"></span> <span class="hidden-xs">Browse</span>')
                            .attr('href', 'javascript:void(0)')
                            .attr('title', 'Open folder in OS File Manager')
                            .on('click', function () {
                                oWebDAV.OpenFolderInOsFileManager(oItem.Href)
                            }));
                    } else {                       
                        actions.push($('<a />')
                            .html('<span class="glyphicon glyphicon-edit" title="Edit"></span> <span class="hidden-xs">Edit</span>')
                            .attr('href', 'javascript:void(0)')
                            .attr('title', 'Edit in associated application')
                            .on('click', function () {
                                oWebDAV.EditDoc(oItem.Href);
                            }));
                    }

                    return actions;
                }

            };

            ///////////////////
            // Search Form View

            var SearchFormView = function (selector) {
                this.$el = $(selector);
                this.Init();
            };
            SearchFormView.prototype = {

                Init: function () {
                    this.$el.find('button').on('click', this._OnSubmit.bind(this));
                    this.$el.find('input')
                            .typeahead({},
                                    {
                                        name: 'states',
                                        display: 'DisplayName',
                                        limit: 6,
                                        templates: {
                                            suggestion: this._RenderSuggestion.bind(this)
                                        },
                                        async: true,
                                        source: this._Source.bind(this)
                                    }
                            )
                            .on('keyup', this._OnKeyUp.bind(this))
                            .on('typeahead:select', this._OnSelect.bind(this));
                },

                SetDisabled: function (bIsDisabled) {
                    this.$el.find('button').prop('disabled', bIsDisabled);
                    this.$el.find('input')
                            .prop('disabled', bIsDisabled)
                            .attr('placeholder', !bIsDisabled ? '' : 'The server does not support search');
                },

                GetValue: function () {
                    return this.$el.find('input.tt-input').val();
                },

                _Source: function (sPhrase, c, fCallback) {
                    oWebDAV.NavigateSearch(sPhrase, false, function (aItems) {
                        fCallback(aItems);
                    });
                },

                _OnKeyUp: function (oEvent) {
                    if (oEvent.keyCode === 13) {
                        oWebDAV.NavigateSearch(oSearchForm.GetValue(), false, function (aItems) {
                            oFolderGrid.Render(aItems, true);
                        });
                        this.$el.find('input').typeahead('close');
                        this._HideKeyboard(this.$el.find('input'));
                    }
                },

                _OnSelect: function (oEvent, oItem) {
                    oFolderGrid.Render([oItem], true);
                },

                _OnSubmit: function () {
                    oWebDAV.NavigateSearch(oSearchForm.GetValue(), false, function (aItems) {
                        oFolderGrid.Render(aItems, true);
                    });
                },

                /**
                 * @param {ITHit.WebDAV.Client.HierarchyItem} oItem
                 **/
                _RenderSuggestion: function (oItem) {
                    var oElement = $('<div />').text(oItem.DisplayName);

                    // Append path
                    new BreadcrumbsView($('<ul />').addClass('breadcrumb').appendTo(oElement)).SetHierarchyItem(oItem);

                    // Append snippet                    
                    oElement.append(Formatters.Snippet(oItem.Properties.Find(oWebDAV.SnippetPropertyName)));

                    return oElement;
                },

                /**
                 * @param {JQuery obeject} element
                 **/
                _HideKeyboard: function (element) {
                    element.attr('readonly', 'readonly'); // Force keyboard to hide on input field.
                    element.attr('disabled', 'true'); // Force keyboard to hide on textarea field.
                    setTimeout(function () {
                        element.blur();  //actually close the keyboard
                        // Remove readonly attribute after keyboard is hidden.
                        element.removeAttr('readonly');
                        element.removeAttr('disabled');
                    }, 100);
                }

            };

            ///////////////////
            // Breadcrumbs View

            var BreadcrumbsView = function (selector) {
                this.$el = $(selector);
            };
            BreadcrumbsView.prototype = {

                /**
                 * @param {ITHit.WebDAV.Client.HierarchyItem} oItem
                 */
                SetHierarchyItem: function (oItem) {
                    var aParts = oItem.Href
                            .split('/')
                            .slice(2)
                            .filter(function (v) {
                                return v;
                            });

                    this.$el.html(aParts.map(function (sPart, i) {
                        var bIsLast = aParts.length === i + 1;
                        var oLabel = i === 0 ? $('<span />').addClass('glyphicon glyphicon-home') : $('<span />').text(decodeURIComponent(sPart));
                        return $('<li />').toggleClass('active', bIsLast).append(
                                bIsLast ?
                                        $('<span />').html(oLabel) :
                                        $('<a />').attr('href', location.protocol + '//' + aParts.slice(0, i + 1).join('/') + '/').html(oLabel)
                        );
                    }));
                }

            };

            /////////////////////////
            // History Api Controller
            var HistoryApiController = function (selector) {
                this.$container = $(selector);
                this.Init();
            };
            HistoryApiController.prototype = {

                Init: function () {
                    if (!this._IsBrowserSupport()) {
                        return;
                    }

                    window.addEventListener('popstate', this._OnPopState.bind(this), false);
                    this.$container.on('click', this._OnLinkClick.bind(this));
                },

                _OnPopState: function (oEvent) {
                    var sUrl = oEvent.state && oEvent.state.Url || location.href;
                    oWebDAV.NavigateFolder(sUrl);
                },

                _OnLinkClick: function (oEvent) {
                    var sUrl = $(oEvent.target).closest('a').attr('href');
                    if (!sUrl) {
                        return;
                    }

                    if (sUrl.indexOf(location.origin) !== 0) {
                        return;
                    }

                    oEvent.preventDefault();

                    history.pushState({ Url: sUrl }, '', sUrl);
                    oWebDAV.NavigateFolder(sUrl);
                },

                _IsBrowserSupport: function () {
                    return !!(window.history && history.pushState);
                }

            };

            /////////////////////
            // WebDAV Controller

            var WebDAVController = function () {
                this.CurrentFolder = null;
                this.WebDavSession = new ITHit.WebDAV.Client.WebDavSession();
                this.SnippetPropertyName = new ITHit.WebDAV.Client.PropertyName('snippet', 'ithit');
            };
            WebDAVController.prototype = {

                Reload: function () {
                    if (this.CurrentFolder) {
                        this.NavigateFolder(this.CurrentFolder.Href);
                    }
                },

                NavigateFolder: function (sPath) {
                    this.WebDavSession.OpenFolderAsync(sPath, [], function (oResponse) {
                        if(oResponse.IsSuccess) {
                            this.CurrentFolder = oResponse.Result;
                            oBreadcrumbs.SetHierarchyItem(this.CurrentFolder);

                            // Detect search support. If search is not supported - disable search field.
                            this.CurrentFolder.GetSupportedFeaturesAsync(function (oResult) {
                                /** @typedef {ITHit.WebDAV.Client.OptionsInfo} oOptionsInfo */
                                var oOptionsInfo = oResult.Result;

                                oSearchForm.SetDisabled(!(oOptionsInfo.Features & ITHit.WebDAV.Client.Features.Dasl));
                            });

                            this.CurrentFolder.GetChildrenAsync(false, [], function (oResult) {
                                if(oResult.IsSuccess) {
                                    /** @type {ITHit.WebDAV.Client.HierarchyItem[]} aItems */
                                    var aItems = oResult.Result;

                                    oFolderGrid.Render(aItems, false);
                                }
                            })
                        }
                    }.bind(this));
                },

                NavigateSearch: function (sPhrase, bIsDynamic, fCallback) {
                    if (!this.CurrentFolder) {
                        fCallback && fCallback([]);
                        return;
                    }

                    if (sPhrase === '') {
                        this.Reload();
                        return;
                    }

                    // The DASL search phrase can contain wildcard characters and escape according to DASL rules:
                    //   ‘%’ – to indicate one or more character.
                    //   ‘_’ – to indicate exactly one character.
                    // If ‘%’, ‘_’ or ‘\’ characters are used in search phrase they are escaped as ‘\%’, ‘\_’ and ‘\\’.
                    var searchQuery = new ITHit.WebDAV.Client.SearchQuery();
                    searchQuery.Phrase = sPhrase.replace(/\\/g, '\\\\').replace(/\%/g, '\\%').replace(/\_/g, '\\_').replace(/\*/g, '%').replace(/\?/g, '_') + '%';
                    searchQuery.EnableContains = !bIsDynamic;  //Enable/disable search in file content.

                    // Get following additional properties from server in search results: snippet - text around search phrase.
                    searchQuery.SelectProperties = [
                        this.SnippetPropertyName
                    ];

                    this.CurrentFolder.SearchByQueryAsync(searchQuery, function (oResult) {
                        /** @type {ITHit.WebDAV.Client.AsyncResult} oResult */

                        /** @type {ITHit.WebDAV.Client.HierarchyItem[]} aItems */
                        var aItems = oResult.Result;

                        fCallback && fCallback(aItems);
                    });
                },

                /**
                 * Opens document for editing.
                 * @param {string} sDocumentUrl Must be full path including domain name: https://webdavserver.com/path/file.ext
                 */
                EditDoc: function (sDocumentUrl) {
                    ITHit.WebDAV.Client.DocManager.EditDocument(sDocumentUrl, this.GetMountUrl(), this._ProtocolInstallMessage.bind(this));
                },

                /**
                 * Opens current folder in OS file manager.
                 */
                OpenCurrentFolderInOsFileManager: function () {
                    this.OpenFolderInOsFileManager(this.CurrentFolder.Href);
                },

                /**
                 * Opens folder in OS file manager.
                 * @param {string} sFolderUrl Must be full path including domain name: https://webdavserver.com/path/
                 */
                OpenFolderInOsFileManager: function (sFolderUrl) {
                    ITHit.WebDAV.Client.DocManager.OpenFolderInOsFileManager(sFolderUrl, this.GetMountUrl(), this._ProtocolInstallMessage.bind(this));
                },

                /**
                 * @return {string}
                 **/
                GetMountUrl: function () {
                    // Web Folders on Windows XP require port, even if it is a default port 80 or 443.
                    var port = window.location.port || (window.location.protocol == 'http:' ? 80 : 443);

                    return window.location.protocol + '//' + window.location.hostname + ':' + port + '<%=Request.ApplicationPath.TrimEnd('/')%>/';
                   },

                   /**
                    * Function to be called when document or OS file manager failed to open.
                    * @private
                    */
                   _ProtocolInstallMessage: function () {
                       if (confirm('This action requires a protocol installation. Select OK to download the protocol installer.')) {

                           // IT Hit WebDAV Ajax Library protocol installers path.
                           // Used to open non-MS Office documents or if MS Office is
                           // not installed as well as to open OS File Manager.
                           var installersFolderPath = 'https://www.ajaxbrowser.com/ITHitService/WebDAVAJAXLibrary/Plugins/';

                           var installerFilePath = installersFolderPath + ITHit.WebDAV.Client.DocManager.GetInstallFileName();
                           window.open(installerFilePath);
                       }
                   }

               };

               var oFolderGrid = new FolderGridView('.ithit-grid-container');
               var oSearchForm = new SearchFormView('.ithit-search-container');
               var oBreadcrumbs = new BreadcrumbsView('.ithit-breadcrumb-container');
               var oHistoryApi = new HistoryApiController('.ithit-grid-container, .ithit-breadcrumb-container');
               var oWebDAV = window.WebDAVController = new WebDAVController();

               // List files on a WebDAV server using WebDAV Ajax Library
               oWebDAV.NavigateFolder(location.href);

               // Set Ajax lib version
               $('.ithit-version-value').text(ITHit.WebDAV.Client.WebDavSession.Version);
               $('.ithit-current-folder-value').text(oWebDAV.GetMountUrl());
           })();
    </script>
    <script>

        // Setting up web socket connection.
        if(location.protocol === "https:") {
            var socketSource = new WebSocket("wss://" + location.host);
        } else {
            var socketSource = new WebSocket("ws://" + location.host);
        }
        

        socketSource.addEventListener('message', function (e) {
            var notifyObject = JSON.parse(e.data);

            // Removing domain and trailing slash.
            var currentLocation = location.pathname.replace(/^\/|\/$/g, '');
            // Checking message type after receiving.
            if (notifyObject.EventType === "refresh") {
                // Refresh folder structure if any item in this folder is updated or new item is created.
                if (currentLocation.toUpperCase() === notifyObject.FolderPath.toUpperCase()) {
                    WebDAVController.Reload();
                }
            } else if (notifyObject.EventType === "delete") {
                if (notifyObject.FolderPath.substring(0, notifyObject.FolderPath.lastIndexOf('/')).toUpperCase() === currentLocation.toUpperCase()) {
                    // Refresh folder structure if any item in this folder is deleted.
                    WebDAVController.Reload();
                } else if (currentLocation.toUpperCase().indexOf(notifyObject.FolderPath.toUpperCase()) === 0) {
                    // Redirect client to the root folder if current path is being deleted.
                    var originPath = location.origin + "/";
                    history.pushState({ Url: originPath }, '', originPath);
                    WebDAVController.NavigateFolder(originPath);
                }
            }
        }, false);

    </script>
</body>
</html>
