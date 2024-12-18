import React from "react";
import DefaultModal from "./DefaultModal";
import { useTranslation } from "react-i18next";
import { useAppDispatch, useAppSelector } from "../../app/hooks/common";
import { getRewriteItemsData, setRewriteItemsData } from "../upload/uploadSlice";

const RewriteModal: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const rewriteItemsData = useAppSelector(getRewriteItemsData);
  const submitModal = () => {
    rewriteItemsData?.onOverwrite();
    closeModal();
  };
  const unsubmitModal = () => {
    rewriteItemsData?.onSkipExists();
    closeModal();
  };
  const closeModal = () => {
    dispatch(setRewriteItemsData(null));
  };

  const getInnerHtml = () => {
    return {
      __html: rewriteItemsData == null ? "" : decodeURI(rewriteItemsData.itemsList),
    };
  };

  return (
    rewriteItemsData && (
      <DefaultModal closeModal={closeModal} title={t("phrases.modals.defaultModalTitle")}>
        <div className="modal-body">
          <p className="message">{t("phrases.validations.followingItemExist")}:</p>
          <p className="message" dangerouslySetInnerHTML={getInnerHtml()}></p>
          <p className="message">{t("phrases.overwrite")}?</p>
        </div>
        <div className="modal-footer">
          <button type="button" className="btn btn-primary btn-ok" onClick={submitModal}>
            {t("phrases.yesToAll")}
          </button>
          <button type="button" className="btn btn-light" onClick={unsubmitModal}>
            {t("phrases.noToAll")}
          </button>
          <button type="button" className="btn btn-light" onClick={closeModal}>
            {t("phrases.cancel")}
          </button>
        </div>
      </DefaultModal>
    )
  );
};

export default RewriteModal;
