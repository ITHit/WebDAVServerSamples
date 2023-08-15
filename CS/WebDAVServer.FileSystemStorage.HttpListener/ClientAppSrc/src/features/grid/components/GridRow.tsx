import React, { useCallback, useState } from "react";
import { useAppSelector, useAppDispatch } from "../../../app/hooks/common";
import { useTranslation } from "react-i18next";
import { CommonService } from "../../../services/CommonService";
import { WebDavService } from "../../../services/WebDavService";
import { ITHit } from "webdav.client";
import {
  addSelectedItem,
  removeSelectedItem,
  getSelectedIndexes,
  getSearchMode,
} from "../gridSlice";
import { StoreWorker } from "../../../app/storeWorker";
import GridRowAction from "./GridRowAction";
import Snippet from "../../search/Snippet";
import { getIsDragging } from "../../upload/uploadSlice";
type Props = {
  item: ITHit.WebDAV.Client.HierarchyItem;
  index: number;
};

const GridRow: React.FC<Props> = ({ item, index }) => {
  const dispatch = useAppDispatch();
  const { t } = useTranslation();
  const isDragging = useAppSelector(getIsDragging);
  const searchMode = useAppSelector(getSearchMode);
  const [snippetHovered, setSnippetHovered] = useState(false);
  let isSelected =
    useAppSelector(getSelectedIndexes).findIndex((el) => el === index) !== -1;
  const handleChangeCheckbox = () => {
    isSelected = !isSelected;
    if (isSelected) {
      dispatch(addSelectedItem(index));
    } else {
      dispatch(removeSelectedItem(index));
    }
  };

  const handleClickLink = useCallback(
    (href: string) => () => {
      StoreWorker.refresh(href);
    },
    []
  );

  const renderLokedIconTooltip = () => {
    var tooltipTitle = "Exclusive lock: " + item.ActiveLocks[0].Owner;
    if (item.ActiveLocks[0].LockScope === "Shared") {
      var userNames = [];
      tooltipTitle =
        "Shared lock" + (item.ActiveLocks.length > 1 ? "(s)" : "") + ": ";
      for (var i = 0; i < item.ActiveLocks.length; i++) {
        userNames.push(item.ActiveLocks[i].Owner);
      }
      tooltipTitle += userNames.join(", ");
    }
    return tooltipTitle;
  };

  return (
    <>
      <tr
        className={`${isDragging ? "table-row-drag" : ""} ${
          WebDavService.isFolder(item) ? "table-row-folder" : "table-row-file"
        } ${snippetHovered ? "hover" : ""}`}
      >
        <td className="select-disabled">
          <label className="custom-checkbox">
            <input
              type="checkbox"
              checked={isSelected}
              onChange={handleChangeCheckbox}
            />
            <span className="checkmark" />
          </label>
        </td>
        <td>
          <span
            className={`${
              WebDavService.isFolder(item) ? "icon icon-folder" : ""
            }`}
          >
            {item.ActiveLocks.length ? (
              <span
                className="icon icon-locked"
                title={renderLokedIconTooltip()}
              >
                {item.ActiveLocks[0].LockScope === "Shared" ? (
                  <span className="badge">{item.ActiveLocks.length}</span>
                ) : (
                  ""
                )}
              </span>
            ) : (
              ""
            )}
          </span>
        </td>
        <td className="ellipsis">
          {WebDavService.isFolder(item) ? (
            <button
              className="btn btn-link"
              onClick={handleClickLink(item.Href)}
            >
              {item.DisplayName}
            </button>
          ) : (
            <span>{item.DisplayName}</span>
          )}
        </td>
        <td className="d-none d-xl-table-cell">
          <span>
            {WebDavService.isFolder(item)
              ? t("phrases.grid.folder")
              : `${t("phrases.grid.file")} ${CommonService.getFileExtension(
                  item.DisplayName
                )}`}
          </span>
        </td>
        <td className="text-right">
          <span>
            {!WebDavService.isFolder(item)
              ? CommonService.formatFileSize(
                  (item as ITHit.WebDAV.Client.File).ContentLength
                )
              : ""}
          </span>
        </td>
        <td className="d-none d-lg-table-cell modified-date">
          {CommonService.formatDate(item.LastModified)}
        </td>
        <td className="text-right select-disabled position-relative">
          <GridRowAction item={item} />
        </td>
      </tr>
      {searchMode && (
        <tr
          className="tr-snippet-url"
          onMouseEnter={() => setSnippetHovered(true)}
          onMouseLeave={() => setSnippetHovered(false)}
        >
          <td className="d-none d-xl-table-cell" />
          <td className="d-none d-lg-table-cell" />
          <td colSpan={10}>
            <Snippet item={item} />
          </td>
        </tr>
      )}
    </>
  );
};

export default GridRow;
