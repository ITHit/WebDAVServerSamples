import React, { useState } from "react";
import Collapse from "react-bootstrap/Collapse";
import { ITHit } from "webdav.client";
import { UrlResolveService } from "../services/UrlResolveService";
import { WebDavSettings } from "../webDavSettings";
import { showProtocolModal } from "./grid/gridSlice";
import { getCurrentFolder } from "./grid/gridSlice";
import { useAppDispatch, useAppSelector } from "../app/hooks/common";
import logo from "/images/logo.svg";
import { ProtocolService } from "../services/ProtocolService";

const Header: React.FC = () => {
  const dispatch = useAppDispatch();

  const [open, setOpen] = useState(false);
  const currentFolder = useAppSelector(getCurrentFolder);

  const showProtocolInstallModal = () => {
    dispatch(showProtocolModal());
  };

  const handleFolderClick = () => {
    if (currentFolder)
      ITHit.WebDAV.Client.DocManager.OpenFolderInOsFileManager(
        currentFolder.Href,
        UrlResolveService.getRootUrl(),
        showProtocolInstallModal,
        null,
        WebDavSettings.EditDocAuth.SearchIn,
        WebDavSettings.EditDocAuth.CookieNames,
        WebDavSettings.EditDocAuth.LoginUrl
      );
  };

  const getInstallerFileUrl = () => {
    return ProtocolService.getInstallerFileUrl();
  };

  const getAjaxLibVersion = () => {
    let innerText;
    if (ITHit.WebDAV.Client.DocManager.IsDavProtocolSupported()) {
      innerText =
        "v" +
        ITHit.WebDAV.Client.WebDavSession.Version +
        ' (<a href="' +
        getInstallerFileUrl() +
        '">Protocol v' +
        ITHit.WebDAV.Client.WebDavSession.ProtocolVersion +
        "</a>)";
    } else {
      innerText =
        "v" +
        ITHit.WebDAV.Client.WebDavSession.Version +
        " (Protocol v" +
        ITHit.WebDAV.Client.WebDavSession.ProtocolVersion +
        ")";
    }
    return innerText;
  };

  return (
    <header>
      <div className="navbar navbar-dark bg-dark shadow-sm">
        <div
          className="container-fluid d-flex justify-content-between js-collapse"
          aria-label="Toggle navigation"
          onClick={() => setOpen(!open)}
          aria-controls="navbarHeader"
          aria-expanded={open}
        >
          <span className="navbar-brand ellipsis">
            <img src={logo} alt="IT Hit logo" className="logo" />
            <span className="brand-name">
              IT Hit WebDAV Server Engine&nbsp;
              <div className="webdav-server-version d-inline">
                {WebDavSettings.WebDavServerVersion}
              </div>
            </span>
          </span>
          <button className="navbar-toggler burger-button" type="button">
            <div className="burger-icon">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </button>
        </div>
      </div>
      <Collapse className="bg-dark" in={open}>
        <div id="navbarHeader">
          <div className="header-content">
            <div className="container-fluid justify-content-between">
              <div className="row">
                <div className="col">
                  <p>
                    This page is displayed when user accesses any folder on your
                    WebDAV server in a web browser. You can customize this page
                    to your needs.
                  </p>
                </div>
              </div>
              <div className="row blocks">
                <div className="col-12 col-lg-4 d-flex flex-column">
                  <h3>Test Your Server</h3>
                  <p>
                    To test your WebDAV server you can run Ajax integration
                    tests right from this page.
                  </p>
                  <span
                    dangerouslySetInnerHTML={{
                      __html: `
                    <button
                    onclick="OpenTestsWindow()"
                    class="align-self-start btn btn-primary"
                  >
                    Run Integration Tests
                  </button>
  `,
                    }}
                  ></span>
                </div>
                <div className="col-12 col-lg-4 d-flex flex-column">
                  <h3>Manage Docs with Ajax File Browser</h3>
                  <p>
                    Use the&nbsp;
                    <a href="https://www.webdavsystem.com/ajaxfilebrowser/programming/">
                      IT Hit Ajax File Browser
                    </a>
                    &nbsp;to browse your documents, open for editing from a web
                    page and - uploading with pause/resume and auto-restore
                    upload.
                  </p>
                  <span
                    dangerouslySetInnerHTML={{
                      __html: `
                    <button
                    onclick="OpenAjaxFileBrowserWindow()"
                    class="align-self-start btn btn-primary"
                  >
                    Browse Using Ajax File Browser
                  </button>
  `,
                    }}
                  ></span>
                </div>
                <div className="col-12 col-lg-4 d-flex flex-column">
                  <h3>Connect with WebDAV Client</h3>
                  <p>
                    Use a WebDAV client provided with almost any OS. Refer
                    to&nbsp;
                    <a href="https://www.webdavsystem.com/server/access">
                      Accessing WebDAV Server
                    </a>
                    &nbsp;page for - detailed instructions. The button below is
                    using&nbsp;
                    <a href="https://www.webdavsystem.com/ajax/">
                      IT Hit WebDAV Ajax Library
                    </a>
                    &nbsp;to mount WebDAV - folder and open the default OS file
                    manager.
                  </p>
                  <button
                    className="align-self-start btn btn-primary"
                    onClick={handleFolderClick}
                  >
                    Browse Using OS File Manager
                  </button>
                </div>
              </div>
              <div className="row mt-1">
                <div className="col">
                  <p className="versions">
                    IT Hit WebDAV Server Engine for .NET:&nbsp;
                    <span className="webdav-server-version">
                      {WebDavSettings.WebDavServerVersion}
                    </span>
                  </p>
                  <p className="versions">
                    IT Hit WebDAV AJAX Library:&nbsp;
                    <span className="ithit-version-value">
                      <span
                        dangerouslySetInnerHTML={{
                          __html: getAjaxLibVersion(),
                        }}
                      ></span>
                    </span>
                  </p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </Collapse>
    </header>
  );
};

export default Header;
