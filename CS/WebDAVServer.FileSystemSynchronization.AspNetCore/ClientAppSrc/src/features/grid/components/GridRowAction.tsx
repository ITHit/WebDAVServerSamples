import React from "react";
import { useTranslation } from "react-i18next";
import { ITHit } from "webdav.client";
import { ProtocolService } from "../../../services/ProtocolService";
import { WebDavService } from "../../../services/WebDavService";
import { UrlResolveService } from "../../../services/UrlResolveService";
import { WebDavSettings } from "../../../webDavSettings";
import { usePopperTooltip } from "react-popper-tooltip";
import { useAppDispatch } from "../../../app/hooks/common";
import { showProtocolModal } from "../gridSlice";
import "react-popper-tooltip/dist/styles.css";
type Props = { item: ITHit.WebDAV.Client.HierarchyItem };

const GridRowAction: React.FC<Props> = ({ item }) => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const { getTooltipProps, setTooltipRef, setTriggerRef, visible } =
    usePopperTooltip({
      trigger: "click",
      placement: "bottom-end",
    });

  const isMicrosoftOfficeDocument = () => {
    return ProtocolService.isMicrosoftOfficeDocument(item.Href);
  };

  const isDisabledAction = (() => {
    return !(
      ProtocolService.isDavProtocolSupported() || isMicrosoftOfficeDocument()
    );
  })();

  const showProtocolInstallModal = () => {
    dispatch(showProtocolModal());
  };

  const handleFolderClick = () => {
    ITHit.WebDAV.Client.DocManager.OpenFolderInOsFileManager(
      item.Href,
      UrlResolveService.getRootUrl(),
      showProtocolInstallModal,
      null,
      WebDavSettings.EditDocAuth.SearchIn,
      WebDavSettings.EditDocAuth.CookieNames,
      WebDavSettings.EditDocAuth.LoginUrl
    );
  };

  const handleEditDocClick = () => {
    if (
      WebDavSettings.EditDocAuth.Authentication &&
      WebDavSettings.EditDocAuth.Authentication.toLowerCase() === "cookies"
    ) {
      ITHit.WebDAV.Client.DocManager.DavProtocolEditDocument(
        item.Href,
        UrlResolveService.getRootUrl(),
        showProtocolInstallModal,
        null,
        WebDavSettings.EditDocAuth.SearchIn,
        WebDavSettings.EditDocAuth.CookieNames,
        WebDavSettings.EditDocAuth.LoginUrl
      );
    } else {
      ITHit.WebDAV.Client.DocManager.EditDocument(
        item.Href,
        UrlResolveService.getRootUrl(),
        showProtocolInstallModal
      );
    }
  };

  const handleOpenDocWithClick = () => {
    ITHit.WebDAV.Client.DocManager.DavProtocolEditDocument(
      item.Href,
      UrlResolveService.getRootUrl(),
      showProtocolInstallModal,
      null,
      WebDavSettings.EditDocAuth.SearchIn,
      WebDavSettings.EditDocAuth.CookieNames,
      WebDavSettings.EditDocAuth.LoginUrl,
      "OpenWith"
    );
  };

  return (
    <>
      {(() => {
        if (WebDavService.isFolder(item)) {
          return (
            <button
              className="btn btn-primary btn-sm btn-labeled"
              type="button"
              disabled={isDisabledAction}
              onClick={handleFolderClick}
            >
              <span className="btn-label">
                <i className="icon icon-open-folder" />
              </span>
              <span className="d-none d-lg-inline-block">
                {t("phrases.browse")}
              </span>
            </button>
          );
        } else {
          return (
            <div className="btn-group">
              <button
                type="button"
                className="btn btn-primary btn-sm btn-labeled btn-default-edit"
                disabled={isDisabledAction}
                onClick={handleEditDocClick}
              >
                <span className="btn-label">
                  <i
                    className={`icon ${
                      isMicrosoftOfficeDocument()
                        ? "icon-microsoft-edit"
                        : "icon-edit"
                    }`}
                  />
                </span>
                <span className="d-none d-lg-inline-block btn-edit-label">
                  {t("phrases.edit")}
                </span>
              </button>
              <button
                className="btn btn-primary dropdown-toggle dropdown-toggle-split btn-sm"
                disabled={isDisabledAction}
                type="button"
                ref={setTriggerRef}
              ></button>
              {visible && (
                <div
                  ref={setTooltipRef}
                  {...getTooltipProps({
                    className: "tooltip popover open",
                  })}
                >
                  <div className="tooltip-inner popover-inner">
                    <div className="dropdown-menu actions show">
                      {isMicrosoftOfficeDocument() && (
                        <button
                          className="dropdown-item"
                          title={t(
                            "phrases.grid.actions.editWithMsOffice.title"
                          )}
                          onClick={handleEditDocClick}
                        >
                          <i className="icon icon-edit-ms-associated" />
                          {t("phrases.grid.actions.editWithMsOffice.text")}
                        </button>
                      )}{" "}
                      {!isMicrosoftOfficeDocument() && (
                        <button
                          className="dropdown-item"
                          title={t(
                            "phrases.grid.actions.editWithDesctopApp.title"
                          )}
                          onClick={handleEditDocClick}
                        >
                          <i className="icon icon-edit-associated" />
                          {t("phrases.grid.actions.editWithDesctopApp.text")}
                        </button>
                      )}
                      <div className="dropdown-divider" />
                      <button
                        className="dropdown-item desktop-app"
                        title={t("phrases.grid.actions.selectDesctopApp.title")}
                        onClick={handleOpenDocWithClick}
                      >
                        {t("phrases.grid.actions.selectDesctopApp.text")}
                      </button>
                    </div>
                  </div>
                </div>
              )}
            </div>
          );
        }
      })()}
    </>
  );
};

export default GridRowAction;
