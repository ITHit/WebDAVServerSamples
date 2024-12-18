import React from "react";
import DefaultModal from "./DefaultModal";
import { useTranslation } from "react-i18next";
type Props = {
  closeModal: () => void;
  submitModal: () => void;
  message: string;
};
const SubmitModal: React.FC<Props> = ({ closeModal, submitModal, message }) => {
  const { t } = useTranslation();
  return (
    <DefaultModal closeModal={closeModal} title={t("phrases.modals.defaultModalTitle")}>
      <div className="modal-body">
        <p className="message">{message}</p>
      </div>
      <div className="modal-footer">
        <button type="button" className="btn btn-primary btn-ok" onClick={submitModal}>
          {t("phrases.ok")}
        </button>
        <button type="button" className="btn btn-light" onClick={closeModal}>
          {t("phrases.cancel")}
        </button>
      </div>
    </DefaultModal>
  );
};

export default SubmitModal;
