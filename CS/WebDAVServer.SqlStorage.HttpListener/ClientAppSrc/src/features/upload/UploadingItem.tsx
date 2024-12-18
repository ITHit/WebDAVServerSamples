import React, { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { ITHit } from "webdav.client";
import { UploadItemRow } from "../../models/UploadItemRow";
import { CommonService } from "../../services/CommonService";
import { UrlResolveService } from "../../services/UrlResolveService";
import UploadingFileIcon from "./UploadingFileIcon";

type Props = {
  uploadItemRow: UploadItemRow;
  onActionItem: () => void;
  onItemStateChanged: () => void;
};
const UploadingItem: React.FC<Props> = ({ uploadItemRow, onActionItem, onItemStateChanged }) => {
  const { t } = useTranslation();
  const { currentState, uploadItem, retryMessage } = uploadItemRow;
  const fileName = uploadItem.GetName();
  const fileExtension = CommonService.getFileExtension(fileName);
  const fileSize = CommonService.formatFileSize(uploadItem.GetProgress().TotalBytes);
  const [progress, setProgress] = useState(0);
  const [speed, setSpeed] = useState(0);

  useEffect(() => {
    const intervalId = setInterval(() => {
      setProgress(uploadItem.GetProgress().Completed);
      setSpeed(uploadItem.GetProgress().Speed);
    }, 500);
    return () => {
      clearInterval(intervalId);
    };
  });

  useEffect(() => {
    onItemStateChanged();
    onActionItem();
  }, [currentState, onActionItem, onItemStateChanged]);

  const isPaused = currentState === ITHit.WebDAV.Client.Upload.State.Paused;
  const [disabledActions, setDisabledActions] = useState(false);

  const pauseUploading = () => {
    setDisabledActions(true);
    uploadItemRow.pauseClickHandler(onActionItem);
    setDisabledActions(false);
  };
  const playUploading = () => {
    setDisabledActions(true);
    uploadItemRow.playClickHandler(onActionItem);
    setDisabledActions(false);
  };
  const cancelUploading = () => {
    setDisabledActions(true);
    uploadItemRow.cancelClickHandler();
    setDisabledActions(false);
  };
  return (
    <>
      {!(
        currentState === ITHit.WebDAV.Client.Upload.State.Completed ||
        currentState === ITHit.WebDAV.Client.Upload.State.Canceled
      ) && (
        <div className="row uploading-item">
          <div className="col-auto px-0">
            <button className="btn-transparent float-start" disabled={disabledActions} onClick={cancelUploading}>
              <i className="icon icon-close"></i>
            </button>
          </div>
          <div className="col-auto px-0">
            <UploadingFileIcon fileExtension={fileExtension} />
          </div>
          <div className="col">
            <div className="row align-items-center">
              <div className="col-auto item-name">{UrlResolveService.decodeUri(fileName)}</div>
              <div className="col-auto item-size">{fileSize}</div>
              {!!retryMessage && <div className="col-auto text-danger">{retryMessage}</div>}
            </div>
            <div className="row">
              <div className="col">
                <div className="progress">
                  <div
                    className="progress-bar"
                    role="progressbar"
                    style={{ width: progress + "%" }}
                    aria-valuenow={progress}
                    aria-valuemin={0}
                    aria-valuemax={100}
                  ></div>
                </div>
              </div>
            </div>
            <div className="row justify-content-between mt-1">
              <div className="col-auto item-progress">
                {progress}% {t("phrases.done")}
              </div>
              <div className="col-auto item-speed">
                {CommonService.formatFileSize(speed) + "/" + t("phrases.secondShortened")}
              </div>
            </div>
          </div>
          <div className="col-auto px-0">
            {!isPaused && (
              <button disabled={disabledActions} className="btn-transparent float-end" onClick={pauseUploading}>
                <i className="icon icon-pause"></i>
              </button>
            )}
            {isPaused && (
              <button disabled={disabledActions} className="btn-transparent float-end" onClick={playUploading}>
                <i className="icon icon-play"></i>
              </button>
            )}
          </div>
        </div>
      )}
    </>
  );
};

export default UploadingItem;
