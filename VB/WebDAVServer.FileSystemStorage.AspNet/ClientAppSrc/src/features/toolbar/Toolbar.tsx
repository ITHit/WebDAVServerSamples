import React, { useState } from "react";
import { toolbarConfig } from "./settings";
import BaseToolbarButton from "./BaseToolbarButton";
import UploadInput from "./UploadInput";
import { getStoredItems, showProtocolModal } from "../grid/gridSlice";
import { useAppDispatch, useAppSelector } from "../../app/hooks/common";
import { WebDavService } from "../../services/WebDavService";
import { ProtocolService } from "../../services/ProtocolService";
import multiDownload from "../../services/MultiDownloadService";
import { StoredType } from "../../models/StoredType";
import { storeSelectedItems } from "../grid/gridSlice";
import CreateFolderModal from "../modals/CreateFolderModal";
import RenameItemModal from "../modals/RenameItemModal";
import SubmitModal from "../modals/SubmitModal";
import { StoreWorker } from "../../app/storeWorker";
import { useTranslation } from "react-i18next";
import { WebDavSettings } from "../../webDavSettings";
import { UrlResolveService } from "../../services/UrlResolveService";
import { ITHit } from "webdav.client";
import { getSelectedItems } from "../../app/storeSelectors";

const Toolbar: React.FC = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  const selectedItems = useAppSelector(getSelectedItems);
  const storedItems = useAppSelector(getStoredItems);

  const [isCreateFolderModalShown, setIsCreateFolderModalShown] =
    useState(false);
  const [isRenameItemModalShown, setIsRenameItemModalShown] = useState(false);
  const [isDeleteItemsModalShown, setIsDeleteItemsModalShown] = useState(false);
  const [isPrintItemsModalShown, setIsPrintItemsModalShown] = useState(false);
  const getButton = (
    btnName: string,
    handleClick: () => void = () => {},
    isDisabled: boolean = false,
    showing: boolean = true
  ) => {
    const btnConfig = toolbarConfig.buttons.find((c) => c.name === btnName);
    if (btnConfig) {
      return (
        <BaseToolbarButton
          handleClick={handleClick}
          isDisabled={isDisabled}
          config={btnConfig}
          showing={showing}
        />
      );
    }
  };

  const getUploadInput = (btnName: string, inputId: string) => {
    const btnConfig = toolbarConfig.buttons.find((c) => c.name === btnName);
    if (btnConfig) {
      return <UploadInput config={btnConfig} inputId={inputId} />;
    }
  };

  const handleDownloadClick = () => {
    multiDownload(
      selectedItems
        .filter((item) => !WebDavService.isFolder(item))
        .map((a) => a.Href + "?download")
    );
  };

  const handleCopyClick = () => {
    storeItems(StoredType.Copy);
  };

  const handleCutClick = () => {
    storeItems(StoredType.Cut);
  };

  const handlePasteClick = () => {
    StoreWorker.pasteStoredItems();
  };

  const handleDeleteItemsClick = () => {
    StoreWorker.deleteSelectedItems();
    setIsDeleteItemsModalShown(false);
  };

  const handlePrintClick = () => {
    StoreWorker.printSelectedItems();
    setIsPrintItemsModalShown(false);
  };

  const handleReloadClick = () => {
    StoreWorker.refresh(null, null, true);
  };

  const handleLockClick = () => {
    lockUnlockDocs(true);
  };

  const handleUnlockClick = () => {
    lockUnlockDocs(false);
  };

  const showProtocolInstallModal = () => {
    dispatch(showProtocolModal());
  };

  const lockUnlockDocs = (lock: boolean) => {
    const filesUrls: string[] = [];

    selectedItems.forEach((item) => {
      if (!WebDavService.isFolder(item)) {
        filesUrls.push(item.Href);
      }
    });

    ITHit.WebDAV.Client.DocManager.DavProtocolEditDocument(
      filesUrls,
      UrlResolveService.getRootUrl(),
      showProtocolInstallModal,
      null,
      WebDavSettings.EditDocAuth.SearchIn,
      WebDavSettings.EditDocAuth.CookieNames,
      WebDavSettings.EditDocAuth.LoginUrl,
      lock ? "Lock" : "Unlock"
    );
  };

  const storeItems = (type: StoredType) => {
    dispatch(storeSelectedItems(type));
  };

  return (
    <div className="sticky-toolbar">
      <div className="ithit-grid-toolbar row">
        <div className="col-auto col-md px-1">
          {getButton("createFolderButton", () => {
            setIsCreateFolderModalShown(true);
          })}
        </div>
        <div className="col-auto px-0 px-xl-2">
          {getButton(
            "downloadButton",
            handleDownloadClick,
            !selectedItems.length ||
              !selectedItems.some((el) => !WebDavService.isFolder(el))
          )}
        </div>
        <div className="col-auto px-0 px-xl-2">
          {getUploadInput("uploadButton", "ithit-button-input")}
        </div>
        <div className="col-auto px-0 px-xl-2">
          {getButton(
            "renameButton",
            () => {
              setIsRenameItemModalShown(true);
            },
            !selectedItems.length || selectedItems.length > 1
          )}
          {getButton(
            "lockButton",
            () => {
              handleLockClick();
            },
            !selectedItems.length ||
              !selectedItems.every((item) => item.ActiveLocks.length === 0),
            !!(
              !selectedItems.every((item) => item.ActiveLocks.length > 0) ||
              !selectedItems.length
            )
          )}
          {getButton(
            "unlockButton",
            () => {
              handleUnlockClick();
            },
            !selectedItems.length ||
              !selectedItems.every((item) => item.ActiveLocks.length > 0),
            !!(
              selectedItems.length &&
              selectedItems.every((item) => item.ActiveLocks.length > 0)
            )
          )}
        </div>
        <div className="col-auto px-0 px-xl-2">
          {getButton("copyButton", handleCopyClick, !selectedItems.length)}
          {getButton("cutButton", handleCutClick, !selectedItems.length)}
          {getButton("pasteButton", handlePasteClick, !storedItems.length)}
        </div>
        <div className="col-auto px-0 px-xl-2">
          {getButton("reloadButton", handleReloadClick)}
        </div>
        <div className="col-auto px-1 px-lg-3">
          {getButton(
            "printButton",
            () => {
              setIsPrintItemsModalShown(true);
            },
            !selectedItems.length ||
              !selectedItems.some((el) => !WebDavService.isFolder(el)) ||
              !ProtocolService.isDavProtocolSupported()
          )}
          {getButton(
            "deleteButton",
            () => {
              setIsDeleteItemsModalShown(true);
            },
            !selectedItems.length
          )}
        </div>
      </div>
      {isCreateFolderModalShown && (
        <CreateFolderModal
          closeModal={() => setIsCreateFolderModalShown(false)}
        />
      )}
      {isRenameItemModalShown && (
        <RenameItemModal closeModal={() => setIsRenameItemModalShown(false)} />
      )}
      {isDeleteItemsModalShown && (
        <SubmitModal
          message={t("phrases.modals.deleteMessage")}
          closeModal={() => setIsDeleteItemsModalShown(false)}
          submitModal={handleDeleteItemsClick}
        />
      )}
      {isPrintItemsModalShown && (
        <SubmitModal
          message={t("phrases.modals.printMessage")}
          closeModal={() => setIsPrintItemsModalShown(false)}
          submitModal={handlePrintClick}
        />
      )}
    </div>
  );
};

export default Toolbar;
