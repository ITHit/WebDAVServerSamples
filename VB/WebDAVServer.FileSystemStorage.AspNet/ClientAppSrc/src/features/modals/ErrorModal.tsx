import React, { useState } from "react";
import DefaultModal from "./DefaultModal";
import { useTranslation } from "react-i18next";
import { ITHit } from "webdav.client";
import { useAppSelector, useAppDispatch } from "../../app/hooks/common";
import { getError, clearError } from "../grid/gridSlice";

const ErrorModal: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const webDavError = useAppSelector(getError);
  const [isOpenedDetails, setIsOpenedDetails] = useState<boolean>(false);

  const closeModal = () => {
    dispatch(clearError());
  };

  const getHtml = (html: string) => {
    return {
      __html: html,
    };
  };

  return (
    <>
      {!!webDavError && (
        <DefaultModal closeModal={closeModal} title={t("phrases.modals.errorTitle")}>
          <div className="modal-body">
            <div className="container-fluid">
              <div className="row">
                <div className="col-md-4">
                  <p>{t("phrases.errors.errorMessage")}:</p>
                </div>
                <div className="col-md-8">
                  <p className="error-message">{webDavError.errorMessage}</p>
                </div>
              </div>
              <div className="row error-details-row">
                <div className="col-md-12">
                  <p>
                    <button
                      className="btn btn-light"
                      type="button"
                      onClick={() => {
                        setIsOpenedDetails(!isOpenedDetails);
                      }}
                    >
                      {t("phrases.errors.errorDetails")}
                    </button>
                  </p>
                  <div id="error-details-collapse" className={`collapse ${isOpenedDetails ? "show" : ""}`}>
                    <div className="card card-body">
                      <div className="row">
                        <div className="col-md-2">
                          <p>{t("phrases.url")}:</p>
                        </div>
                        <div className="col-md-10">
                          {webDavError.error &&
                            webDavError.error instanceof ITHit.WebDAV.Client.Exceptions.WebDavHttpException && (
                              <p
                                className="error-details-url"
                                dangerouslySetInnerHTML={getHtml(webDavError.error.Uri)}
                              />
                            )}
                        </div>
                      </div>
                      <div className="row">
                        <div className="col-md-4 ">
                          <p>{t("phrases.errors.errorMessage")}:</p>
                        </div>
                        <div className="col-md-8">
                          <p
                            className="error-details-message"
                            dangerouslySetInnerHTML={getHtml(webDavError.getServerMessage())}
                          />
                        </div>
                      </div>
                    </div>
                  </div>
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

export default ErrorModal;
