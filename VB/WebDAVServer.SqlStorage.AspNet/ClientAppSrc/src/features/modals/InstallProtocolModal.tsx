import React, { useState } from "react";
import DefaultModal from "./DefaultModal";
import { useTranslation } from "react-i18next";
import { ProtocolService } from "../../services/ProtocolService";
import { useAppDispatch, useAppSelector } from "../../app/hooks/common";
import { hideProtocolModal, getProtocolModalDisplayed } from "../grid/gridSlice";

const InstallProtocolModal: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const protocolModalDisplayed = useAppSelector(getProtocolModalDisplayed);
  const webDavProtocol = ProtocolService.getProtocol();
  const [moreOsDisplayed, setMoreOsDisplayed] = useState<boolean>(false);
  const [moreBrowsersDisplayed, setMoreBrowsersDisplayed] = useState<boolean>(false);
  const closeModal = () => {
    dispatch(hideProtocolModal());
  };
  return (
    <>
      {protocolModalDisplayed && (
        <DefaultModal
          dialogClassName="modal-lg"
          closeModal={closeModal}
          title={t("phrases.modals.downloadProtocolTitle")}
        >
          <div className="modal-body">
            <div className="container-fluid">
              <div className="row">
                <div className="col-md-12">
                  <p>{t("phrases.downloadProtocol.installCustomProtocol")}</p>
                  <ol>
                    <li>
                      {t("phrases.downloadProtocol.downloadAndInstallFiles")}:
                      <br />
                      {!!webDavProtocol.currentOs && (
                        <span className="current-os">
                          <span className={webDavProtocol.currentOs.cssClass}>
                            <span>{webDavProtocol.currentOs.name}</span>
                            <a target="_blank" href={webDavProtocol.currentOs.downloadLink} rel="noreferrer">
                              {webDavProtocol.currentOs.fileName}
                            </a>
                            <br />
                          </span>
                        </span>
                      )}
                      <button className="btn btn-link more-lnk" onClick={() => setMoreOsDisplayed(!moreOsDisplayed)}>
                        <span>{moreOsDisplayed ? "- " : "+"}</span>
                        {t("phrases.otherOs")}:
                      </button>
                      <p className={`more-pnl ${moreOsDisplayed ? "d-block" : ""}`}>
                        {webDavProtocol.otherOs.map((item, index) => {
                          return (
                            <span key={"os-" + index} className={item.cssClass}>
                              <span>{item.name}</span>
                              <a target="_blank" href={item.downloadLink} rel="noreferrer">
                                {item.fileName}
                              </a>
                              <br />
                            </span>
                          );
                        })}
                      </p>
                    </li>
                    <li>
                      {t("phrases.downloadProtocol.enableITHitEditDocumentOpener")}
                      :
                      <br />
                      <span className="not-required-internet-explorer" style={{ display: "none" }}>
                        {t("phrases.downloadProtocol.notRequiredForInternetExplorer")}
                        .<br />
                      </span>
                      {!!webDavProtocol.currentBrowser && (
                        <span className="current-browser">
                          <span className={webDavProtocol.currentBrowser.cssClass}>
                            <span>{webDavProtocol.currentBrowser.name}</span>
                            {webDavProtocol.currentBrowser.downloadLink && (
                              <a target="_blank" href={webDavProtocol.currentBrowser.downloadLink} rel="noreferrer">
                                {webDavProtocol.currentBrowser.fileName}
                              </a>
                            )}

                            <br />
                          </span>
                        </span>
                      )}
                      <button
                        className="btn btn-link more-lnk"
                        onClick={() => setMoreBrowsersDisplayed(!moreBrowsersDisplayed)}
                      >
                        <span>{moreBrowsersDisplayed ? "- " : "+"}</span>
                        {t("phrases.otherWebBrowsers")}:
                      </button>
                      <p className={`more-pnl ${moreBrowsersDisplayed ? "d-block" : ""}`}>
                        {webDavProtocol.otherBrowsers.map((item, index) => {
                          return (
                            <span key={"browser" + index} className={item.cssClass}>
                              <a target="_blank" href={item.downloadLink} rel="noreferrer">
                                {item.fileName}
                              </a>
                              <br />
                            </span>
                          );
                        })}
                      </p>
                    </li>
                  </ol>
                  <br />
                  {t("phrases.see")}{" "}
                  <a
                    href="https://www.webdavsystem.com/ajax/programming/open-doc-webpage/install/web_browser_extensions/"
                    target="_blank"
                    rel="noreferrer"
                  >
                    {t("phrases.downloadProtocol.installAndVerifyExtensions")}.
                  </a>
                </div>
              </div>
            </div>
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-light" onClick={closeModal}>
              {t("phrases.close")}
            </button>
          </div>
        </DefaultModal>
      )}
    </>
  );
};

export default InstallProtocolModal;
