import React, { useCallback } from "react";
import { StoreWorker } from "../app/storeWorker";
import { UrlResolveService } from "../services/UrlResolveService";
import { useAppSelector } from "../app/hooks/common";
import { getCurrentFolder, getCurrentUrl } from "./grid/gridSlice";
import { useTranslation } from "react-i18next";
import { useOpenFolderInFileManagerClick } from "../app/hooks/useOpenFolderInFileManagerClick";
import DownloadDriveButton from "./DownloadDriveButton";
type Props = { itemUrl?: string; isSearchMode: boolean };
const Breadcrumb: React.FC<Props> = ({ itemUrl, isSearchMode }) => {
  const { t } = useTranslation();
  const currentUrl = useAppSelector(getCurrentUrl);
  const currentFolder = useAppSelector(getCurrentFolder);
  const { handleOpenFolderInFileManagerClick } = useOpenFolderInFileManagerClick();
  const rootUrl = UrlResolveService.getRootUrl();
  let url = "";

  if (!itemUrl) {
    url = UrlResolveService.getTail(currentUrl, rootUrl);
  } else {
    url = UrlResolveService.getTail(itemUrl, rootUrl);
  }

  const parts = url
    .split("/")
    .slice()
    .filter(function (v) {
      return v;
    });

  const getHref = useCallback(
    (index: number) => {
      return "/" + parts.slice(0, index + 1).join("/") + "/";
    },
    [parts]
  );

  const handleUpOneLevelClick = () => {
    const tail = parts.length >= 2 ? getHref(parts.length - 2) : "";
    let upOneLevelUrl = rootUrl;
    if (rootUrl[rootUrl.length - 1] !== "/" && !tail) {
      upOneLevelUrl += "/";
    } else {
      upOneLevelUrl += tail;
    }
    StoreWorker.refresh(upOneLevelUrl);
  };

  const handleHomeClick = () => {
    let homeUrl = rootUrl;
    if (rootUrl[rootUrl.length - 1] !== "/") {
      homeUrl += "/";
    }
    StoreWorker.refresh(homeUrl);
  };

  const handleItemClick = useCallback(
    (index: number) => () => {
      StoreWorker.refresh(rootUrl + getHref(index));
    },
    [getHref, rootUrl]
  );
  return (
    <nav aria-label="breadcrumb" className={isSearchMode ? "snippet-breadcrumb" : ""}>
      <div className="ithit-breadcrumb-container">
        {!isSearchMode && (
          <button
            className="btn-tool"
            onClick={handleUpOneLevelClick}
            disabled={!parts.length}
            title={t("phrases.breadcrumb.upOneLevelTitle")}
          >
            <i className="icon icon-up-one-level"></i>
          </button>
        )}
        <ol className="breadcrumb">
          <li className="breadcrumb-item">
            {!isSearchMode && (
              <button className="btn p-0" onClick={handleHomeClick}>
                <i className="icon icon-home"></i>
              </button>
            )}
          </li>
          {parts.map((item, i) => {
            return (
              <li key={"breadcrumb-item-" + i} className={`breadcrumb-item ${i === parts.length - 1 ? "active" : ""}`}>
                {i !== parts.length - 1 ? (
                  <button className="btn btn-link" onClick={handleItemClick(i)}>
                    <span>{decodeURIComponent(item)}</span>
                  </button>
                ) : (
                  <span>{decodeURIComponent(item)}</span>
                )}
              </li>
            );
          })}
        </ol>
        <div className="feature-buttons">
          <button
            onClick={() => handleOpenFolderInFileManagerClick(currentFolder?.Href)}
            className="btn btn-primary"
            type="button"
            title="Browse Using OS File Manager"
          >
            Browse Using OS File Manager
          </button>
          {!isSearchMode && <DownloadDriveButton />}
        </div>
      </div>
    </nav>
  );
};

export default Breadcrumb;
