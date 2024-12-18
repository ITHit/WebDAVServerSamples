import React, { useState } from "react";
import DefaultModal from "./DefaultModal";
import { useTranslation } from "react-i18next";
import { useAppSelector } from "../../app/hooks/common";
import { CommonService } from "../../services/CommonService";
import { StoreWorker } from "../../app/storeWorker";
import { getSelectedItems } from "../../app/storeSelectors";

type Props = { closeModal: () => void };

const RenameItemModal: React.FC<Props> = ({ closeModal }) => {
  const { t } = useTranslation();
  const selectedItem = useAppSelector(getSelectedItems)[0];

  const [errorMessage, setErrorMessage] = useState<string | undefined>("");
  const [itemName, setItemName] = useState(selectedItem.DisplayName);
  const oldItemName = selectedItem.DisplayName;

  const renameItem = () => {
    StoreWorker.renameSelectedItem(itemName);
    closeModal();
  };

  const handleSubmit = () => {
    if (oldItemName === itemName) {
      closeModal();
    } else if (itemName != null && itemName.match(/^ *$/) === null) {
      setErrorMessage(CommonService.validateName(itemName));
      if (!errorMessage) {
        renameItem();
      }
    } else {
      setErrorMessage("phrases.validations.nameIsRequired");
    }
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const enteredName = event.target.value;
    setItemName(enteredName);
  };
  return (
    <DefaultModal closeModal={closeModal} title={t("phrases.modals.renameItemTitle")}>
      <form onSubmit={handleSubmit}>
        <div className="modal-body">
          <div className="form-group">
            <input
              value={itemName}
              autoFocus
              type="text"
              className="form-control"
              placeholder={t("phrases.modals.itemNamePlaceholder")}
              onChange={handleInputChange}
            />
            {errorMessage && <div className="alert alert-danger">{errorMessage}</div>}
          </div>
        </div>
        <div className="modal-footer">
          <button type="submit" className="btn btn-primary btn-submit">
            {t("phrases.ok")}
          </button>
          <button type="button" className="btn btn-light" onClick={closeModal}>
            {t("phrases.cancel")}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
};

export default RenameItemModal;
