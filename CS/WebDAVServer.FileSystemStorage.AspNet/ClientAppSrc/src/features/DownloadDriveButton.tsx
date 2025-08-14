import React, { useState } from "react";
import DownloadDriveModal from "./modals/DownloadDriveModal";
import { WebDavSettings } from "../webDavSettings";
import { ProtocolService } from "../services/ProtocolService";

const DownloadDriveButton: React.FC = () => {
  const [showDownloadModal, setShowDownloadModal] = useState(false);
  const driveName = WebDavSettings.DriveProjectName || "WebDAV Drive";
  const handleButtonClick = () => {
    if (WebDavSettings.IsIntegratedProject) {
      setShowDownloadModal(true);
    } else {
      window.open(ProtocolService.getInstallerFileUrl(), "_blank");
    }
  };
  return (
    <>
      <button
        id="ithit-webdav-drive"
        onClick={() => handleButtonClick()}
        className="btn btn-primary btn-sm btn-labeled"
        type="button"
        title="Download WebDAV Drive application."
      >
        <span className="btn-label">
          <i className="icon-webdav-drive"></i>
        </span>
        <span className="d-none d-lg-inline-block">Download {driveName}</span>
      </button>
      {WebDavSettings.IsIntegratedProject && (
        <DownloadDriveModal
          show={showDownloadModal}
          onHide={() => setShowDownloadModal(false)}
          appName={WebDavSettings.DriveProjectName}
        />
      )}
    </>
  );
};

export default DownloadDriveButton;
