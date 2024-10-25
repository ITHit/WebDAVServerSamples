import React, { useCallback } from "react";
import { StoreWorker } from "../app/storeWorker";
import { UrlResolveService } from "../services/UrlResolveService";
import { useAppSelector } from "../app/hooks/common";
import { getCurrentUrl } from "./grid/gridSlice";
import { useTranslation } from "react-i18next";
import { ProtocolService } from "../services/ProtocolService";
type Props = { itemUrl?: string; isSearchMode: boolean };
const Breadcrumb: React.FC<Props> = ({ itemUrl, isSearchMode }) => {
  const { t } = useTranslation();
  const currentUrl = useAppSelector(getCurrentUrl);
  const rootUrl = UrlResolveService.getRootUrl();
  let url = "";

  if (!itemUrl) {
    url = UrlResolveService.getTail(currentUrl, rootUrl);
  } else {
    url = UrlResolveService.getTail(itemUrl, rootUrl);
  }

  let parts = url
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
    let tail = parts.length >= 2 ? getHref(parts.length - 2) : "";
    let upOneLevelUrl = rootUrl;
    if (rootUrl[rootUrl.length - 1] !== "/" && !tail) {
      upOneLevelUrl += "/";
    } else {
      upOneLevelUrl += tail;
    }
    StoreWorker.refresh(upOneLevelUrl);
  };

  const handleHomeClick = () => {
    var homeUrl = rootUrl;
    if (rootUrl[rootUrl.length - 1] !== "/") {
      homeUrl += "/";
    }
    StoreWorker.refresh(homeUrl);
  };
  
  const getInstallerFileUrl = () => {
    return ProtocolService.getInstallerFileUrl();
  };

  const handleItemClick = useCallback(
    (index: number) => () => {
      StoreWorker.refresh(rootUrl + getHref(index));
    },
    [getHref, rootUrl]
  );
  return (
    <nav
      aria-label="breadcrumb"
      className={isSearchMode ? "snippet-breadcrumb" : ""}
    >
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
              <li
                key={"breadcrumb-item-" + i}
                className={`breadcrumb-item ${
                  i === parts.length - 1 ? "active" : ""
                }`}
              >
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
        <a
        id="ithit-webdav-drive"
        href={getInstallerFileUrl()}
        className="btn btn-primary btn-sm btn-labeled"
        type="button"
        title="Download WebDAV Drive application."
      >
        <span className="btn-label"><i className="icon-webdav-drive"></i></span>
        <span className="d-none d-lg-inline-block">Download WebDAV Drive</span>
      </a>
      </div>
    </nav>
  );
};

export default Breadcrumb;
