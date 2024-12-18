import React, { useCallback, useEffect, useState } from "react";
import { ITHit } from "webdav.client";
import { useAppSelector } from "../../app/hooks/common";
import { getUploadItemRows } from "./uploadSlice";
import { useTranslation } from "react-i18next";
import UploadingItem from "./UploadingItem";
import Draggable from "react-draggable";

const Uploader: React.FC = () => {
  const { t } = useTranslation();
  const uploadItemRows = useAppSelector(getUploadItemRows);
  const [showDetails, setShowDetails] = useState<boolean>(false);
  const [isUploadingHover, setIsUploadingHover] = useState(false);
  const [isFirstShowBlock, setIsFirstShowBlock] = useState(true);
  const [isPaused, setIsPaused] = useState(false);
  const [uploadedPersent, setUploadedPersent] = useState(0);
  const [isShowUploading, setIsShowUploading] = useState(false);

  const isUploading = useCallback(() => {
    let isShow = false;
    uploadItemRows.forEach((el) => {
      if (
        !(
          el.currentState === ITHit.WebDAV.Client.Upload.State.Completed ||
          el.currentState === ITHit.WebDAV.Client.Upload.State.Canceled
        )
      ) {
        isShow = true;
      }
    });
    return isShow;
  }, [uploadItemRows]);

  useEffect(() => {
    setIsShowUploading(isUploading());

    if (isFirstShowBlock && isShowUploading) {
      setTimeout(() => {
        setIsFirstShowBlock(false);
      }, 4000);
    }

    const getUploadedPersent = () => {
      let uploaded = 0;
      if (uploadItemRows.length) {
        uploadItemRows.forEach((element) => {
          uploaded += element.uploadItem.GetProgress().Completed;
        });
        uploaded /= uploadItemRows.length;
      }

      return Math.round(uploaded);
    };

    const intervalId = setInterval(() => {
      setUploadedPersent(getUploadedPersent());
    }, 500);
    return () => {
      clearInterval(intervalId);
    };
  }, [isUploading, isFirstShowBlock, isShowUploading, uploadItemRows]);

  const toggleDetails = () => {
    setShowDetails(!showDetails);
  };

  const pauseAll = () => {
    uploadItemRows.forEach((el) => {
      el.pauseClickHandler();
    });
    setIsPaused(true);
  };

  const playAll = () => {
    uploadItemRows.forEach((el) => {
      if (el.currentState === ITHit.WebDAV.Client.Upload.State.Paused) {
        el.playClickHandler();
      }
    });
    setIsPaused(false);
  };

  const cancelAll = () => {
    uploadItemRows.forEach((el) => {
      el.cancelClickHandler();
    });
  };

  const onActionItem = () => {
    let countPaused = 0;
    let countCompleted = 0;
    uploadItemRows.forEach((el) => {
      if (el.currentState === ITHit.WebDAV.Client.Upload.State.Paused) {
        countPaused++;
      } else if (
        el.currentState === ITHit.WebDAV.Client.Upload.State.Completed ||
        el.currentState === ITHit.WebDAV.Client.Upload.State.Canceled
      ) {
        countCompleted++;
      }
    });

    if (countPaused === 0) {
      setIsPaused(false);
    } else if (countPaused === uploadItemRows.length - countCompleted) {
      setIsPaused(true);
    }
  };

  const onItemStateChanged = () => {
    setIsShowUploading(isUploading());
  };

  return (
    <>
      {!!uploadItemRows.length && isShowUploading && (
        <div className="uploading">
          <div
            className="progress-wrapper"
            onMouseOver={() => {
              setIsUploadingHover(true);
            }}
            onMouseLeave={() => {
              setIsUploadingHover(false);
            }}
          >
            <div className="progress">
              <div
                className="progress-bar"
                role="progressbar"
                style={{ width: uploadedPersent + "%" }}
                aria-valuenow={uploadedPersent}
                aria-valuemin={0}
                aria-valuemax={100}
              ></div>
            </div>
          </div>
          <div
            className={`uploading-block ${(isUploadingHover || isFirstShowBlock) && !showDetails ? "show" : ""} ${
              showDetails ? "d-none" : ""
            }`}
          >
            <div className="uploading-controls">
              {t("phrases.uploader.uploaded")}
              <span className="persent"> {uploadedPersent}%</span>
              {!isPaused && (
                <button className="btn-transparent float-end" title="Pause upload" onClick={pauseAll}>
                  <i className="icon icon-pause"></i>
                </button>
              )}
              {isPaused && (
                <button className="btn-transparent float-end" title="Resume upload" onClick={playAll}>
                  <i className="icon icon-play"></i>
                </button>
              )}
            </div>
            <div>
              <button className="btn btn-primary" onClick={toggleDetails}>
                {t("phrases.details")}
              </button>
            </div>
          </div>
          <div className={showDetails ? "" : "d-none"}>
            <Draggable>
              <div className="uploading-details" style={{ position: "fixed" }}>
                <div className="details-header text-center">
                  <span className="details-title">{t("phrases.uploader.filesUpload")}</span>
                  <button
                    className="btn-transparent float-end"
                    //:disabled="disabledPauseButton"
                    title={t("phrases.close")}
                    onClick={toggleDetails}
                  >
                    <i className="icon icon-close"></i>
                  </button>
                </div>
                <div className="uploading-items">
                  {uploadItemRows.map((item, i) => {
                    return (
                      <UploadingItem
                        key={"uploading-item-" + i}
                        uploadItemRow={item}
                        onActionItem={onActionItem}
                        onItemStateChanged={onItemStateChanged}
                      />
                    );
                  })}
                </div>
                <div className="uploading-footer">
                  {!isPaused && (
                    <button
                      className="btn btn-outline-primary"
                      //:disabled="disabledPauseButton"
                      title={t("phrases.uploader.pauseUpload")}
                      onClick={pauseAll}
                    >
                      {t("phrases.uploader.pauseUpload")}
                    </button>
                  )}
                  {isPaused && (
                    <button
                      className="btn btn-outline-primary"
                      //:disabled="disabledPauseButton"
                      title={t("phrases.uploader.resumeUpload")}
                      onClick={playAll}
                    >
                      {t("phrases.uploader.resumeUpload")}
                    </button>
                  )}
                  <button className="btn btn-primary" title={t("phrases.uploader.cancelAllUpload")} onClick={cancelAll}>
                    {t("phrases.uploader.cancelAllUpload")}
                  </button>
                </div>
              </div>
            </Draggable>
          </div>
        </div>
      )}
    </>
  );
};

export default Uploader;
