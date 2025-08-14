import React from "react";
import DefaultModal from "./DefaultModal";
import { UrlResolveService } from "../../services/UrlResolveService";

interface Props {
  show: boolean;
  onHide: () => void;
  appName?: string;
}

const DownloadDriveModal: React.FC<Props> = ({ show, onHide, appName }) => {
  return (
    <>
      {show && (
        <DefaultModal title={`Download ${appName}`} closeModal={onHide}>
          <div className="modal-body">
            <div className="alert alert-warning mb-4" role="alert">
              Your <strong>{appName}</strong> is signed with a development certificate.
            </div>

            <div className="mb-4">
              <h6>For Testing:</h6>
              <p className="text-muted mb-3">
                To install the application for testing purposes download zip archive and run the "Install.ps1".
              </p>

              <h6>For Production:</h6>
              <p className="text-muted mb-3">
                To deploy the application in production environment use the Package and Publish in your Drive project context menu.
              </p>
            </div>
          </div>
          <div className="modal-footer justify-content-center">
            <a href={`${UrlResolveService.getServerRootUrl()}/WebDAVDrive.zip`} className="btn btn-primary">
              Download zip for Testing
            </a>
            <a href={`${UrlResolveService.getServerRootUrl()}/WebDAVDrive.msix`} className="btn btn-primary">
              Download msix for Production
            </a>
          </div>
        </DefaultModal>
      )}
    </>
  );
};

export default DownloadDriveModal;
