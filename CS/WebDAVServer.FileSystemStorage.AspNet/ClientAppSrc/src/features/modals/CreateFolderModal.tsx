import React, { useState } from "react";
import DefaultModal from "./DefaultModal";
import { useTranslation } from "react-i18next";
import { CommonService } from "../../services/CommonService";
import { WebDavService } from "../../services/WebDavService";
import { ITHit } from "webdav.client";
import { getCurrentFolder } from "../grid/gridSlice";
import { useAppSelector } from "../../app/hooks/common";
import { StoreWorker } from "../../app/storeWorker";
type Props = { closeModal: () => void };
const CreateFolderModal: React.FC<Props> = ({ closeModal }) => {
  const { t } = useTranslation();
  const currentFolder = useAppSelector(getCurrentFolder);
  const [errorMessage, setErrorMessage] = useState<string | undefined>("");
  const [folderName, setFolderName] = useState("");
  const createFolder = () => {
    if (currentFolder !== null)
      WebDavService.createFolder(currentFolder, folderName)
        .then(() => {
          StoreWorker.refresh();
          closeModal();
        })
        .catch((resp) => {
          if (resp.Error instanceof ITHit.WebDAV.Client.Exceptions.MethodNotAllowedException) {
            setErrorMessage(
              resp.Error.Error.Description ? resp.Error.Error.Description : t("phrases.validations.folderExists")
            );
          }
        });
  };

  const handleSubmit = (event: React.SyntheticEvent) => {
    if (folderName != null && folderName.match(/^ *$/) === null) {
      setErrorMessage(CommonService.validateName(folderName));
      if (!errorMessage) {
        createFolder();
      }
    } else {
      setErrorMessage(t("phrases.validations.nameIsRequired"));
    }

    event.preventDefault();
  };

  const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>) => {
    const enteredName = event.target.value;
    setFolderName(enteredName);
  };

  return (
    <DefaultModal title={t("phrases.modals.createFolderTitle")} closeModal={closeModal}>
      <form onSubmit={handleSubmit}>
        <div className="modal-body">
          <div className="form-group">
            <input
              value={folderName}
              autoFocus
              type="text"
              className="form-control"
              placeholder={t("phrases.modals.folderNamePlaceholder")}
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

export default CreateFolderModal;
