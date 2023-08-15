import React from "react";
import { useAppSelector, useAppDispatch } from "../../../app/hooks/common";
import TableSortHeader from "./TableSortHeader";
import { useTranslation } from "react-i18next";
import {
  clearSelectedItems,
  addAllSelectedItems,
  getAllSelected,
} from "../gridSlice";
type Props = {};
const GridHeader: React.FC<Props> = () => {
  const { t } = useTranslation();
  const dispatch = useAppDispatch();
  let allSelected = useAppSelector(getAllSelected);

  const handleChangeCheckbox = () => {
    allSelected = !allSelected;
    if (allSelected) {
      dispatch(addAllSelectedItems());
    } else {
      dispatch(clearSelectedItems());
    }
  };

  return (
    <thead>
      <tr>
        <th scope="col">
          <label className="custom-checkbox">
            <input
              checked={allSelected}
              onChange={handleChangeCheckbox}
              type="checkbox"
            />
            <span className="checkmark" />
          </label>
        </th>
        <th scope="col" />
        <TableSortHeader cssClass="ellipsis sort" fieldName="displayname">
          <span>{t("phrases.grid.tableHeader.displayName")}</span>
        </TableSortHeader>
        <TableSortHeader
          cssClass="d-none d-xl-table-cell sort"
          fieldName="getcontenttype"
        >
          <span>{t("phrases.grid.tableHeader.type")}</span>
        </TableSortHeader>
        <TableSortHeader cssClass="sort" fieldName="quota-used-bytes">
          <span>{t("phrases.grid.tableHeader.size")}</span>
        </TableSortHeader>
        <TableSortHeader
          cssClass="d-none d-lg-table-cell sort"
          fieldName="getlastmodified"
        >
          <span>{t("phrases.grid.tableHeader.modified")}</span>
        </TableSortHeader>
        <th className="column-action" scope="col" />
      </tr>
    </thead>
  );
};

export default GridHeader;
