import { useAppSelector, useAppDispatch } from "../../../app/hooks/common";
import { setSortColumn, setSortAscending, getSortColumn, getSortAscending } from "../gridSlice";
import { StoreWorker } from "../../../app/storeWorker";
import { ReactNode } from "react";

type Props = {
  children: ReactNode;
  cssClass: string;
  fieldName: string;
};

const TableSortHeader: React.FC<Props> = ({ children, cssClass, fieldName }) => {
  const storeSortColumn = useAppSelector(getSortColumn);
  const storeSortAscending = useAppSelector(getSortAscending);

  const getCssClass = () => {
    let className = cssClass;
    if (fieldName === storeSortColumn) {
      className += " ";
      className += storeSortAscending ? "ascending" : "descending";
    }

    return className;
  };

  const dispatch = useAppDispatch();
  const handleClick = () => {
    if (fieldName !== storeSortColumn) {
      dispatch(setSortColumn(fieldName));
      dispatch(setSortAscending(true));
    } else {
      dispatch(setSortAscending(!storeSortAscending));
    }

    StoreWorker.refresh();
  };

  return (
    <th className={getCssClass()} scope="col" onClick={handleClick}>
      {children}
    </th>
  );
};

export default TableSortHeader;
