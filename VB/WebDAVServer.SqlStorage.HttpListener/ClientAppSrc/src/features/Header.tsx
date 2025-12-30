import React, { useState } from "react";
import Collapse from "react-bootstrap/Collapse";
import { WebDavSettings } from "../webDavSettings";
import { getCurrentFolder } from "./grid/gridSlice";
import { useAppSelector } from "../app/hooks/common";
import { useOpenFolderInFileManagerClick } from "../app/hooks/useOpenFolderInFileManagerClick";
import { ITHit } from "webdav.client";
import logo from "/images/logo.svg";

const Header: React.FC = () => {
  const [open, setOpen] = useState(false);
  const currentFolder = useAppSelector(getCurrentFolder);
  const { handleOpenFolderInFileManagerClick } = useOpenFolderInFileManagerClick();

  const handleOpenTestsWindow = () => {
    const width = Math.round(screen.width * 0.5);
    const height = Math.round(screen.height * 0.8);
    window.open(
      `/AjaxFileBrowser/AjaxIntegrationTests.html#${WebDavSettings.WebsiteRootUrl}`,
      "",
      `menubar=1,location=1,status=1,scrollbars=1,resizable=1,width=${width},height=${height}`
    );
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
              <div className="webdav-server-version d-inline">{WebDavSettings.WebDavServerVersion}</div>
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
                    This page is displayed when user accesses any folder on your WebDAV server in a web browser. You can
                    customize this page to your needs.
                  </p>
                </div>
              </div>
              <div className="row blocks">
                <div className="col-12 col-lg-4 d-flex flex-column">
                  <h3>Software Used in This Solution</h3>
                  <ul className="mb-0">
                    <li>
                      <a href="https://www.webdavsystem.com/server/" target="_blank">
                        IT Hit WebDAV Server Engine for .NET
                      </a>
                      :&nbsp;
                      <span className="webdav-server-version">{WebDavSettings.WebDavServerVersion}</span>
                    </li>
                    <li>
                      <a href="https://www.webdavsystem.com/ajax/" target="_blank">
                        IT Hit WebDAV AJAX Library
                      </a>
                      : {ITHit.WebDAV.Client.WebDavSession.Version}
                    </li>
                    {WebDavSettings.IsIntegratedProject && (
                      <>
                        <li>
                          <a href="https://www.userfilesystem.com/" target="_blank">
                            IT Hit User File System
                          </a>
                          : v9.0.29527
                        </li>
                        <li>
                          <a href="https://www.webdavsystem.com/client/" target="_blank">
                            IT Hit WebDAV Client Library
                          </a>
                          : v7.1.5044
                        </li>
                      </>
                    )}
                  </ul>
                </div>
                <div className="col-12 col-lg-4 d-flex flex-column">
                  <h3>Test Your Server</h3>
                  <p>To test your WebDAV server you can run Ajax integration tests right from this page.</p>
                  <button onClick={handleOpenTestsWindow} className="align-self-start btn btn-primary mt-auto">
                    Run Integration Tests
                  </button>
                </div>
                <div className="col-12 col-lg-4 d-flex flex-column">
                  <h3>Mount WebDAV Drive</h3>
                  <p>Install WebDAV Drive to manage files in OS File Manager and open documents for editing.</p>
                  <button
                    className="align-self-start btn btn-primary mt-auto"
                    onClick={() => handleOpenFolderInFileManagerClick(currentFolder?.Href)}
                  >
                    Browse Using OS File Manager
                  </button>
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
