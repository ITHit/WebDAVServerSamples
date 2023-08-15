import React from "react";
import { useAppSelector } from "../../../app/hooks/common";
import { getPageSize } from "../gridSlice";
type Props = {};

const SceletonGridContainer: React.FC<Props> = () => {
  const pageSize = useAppSelector(getPageSize);
  return (
    <table className="table table-hover ithit-grid-container">
      <thead>
        <tr>
          <th scope="col">
            <label className="custom-checkbox">
              <input type="checkbox" disabled={true} />
              <span className="checkmark" />
            </label>
          </th>
          <th scope="col"></th>
          <th className="ellipsis sort" scope="col"></th>
          <th className="d-none d-xl-table-cell sort" scope="col"></th>
          <th className="sort" scope="col"></th>
          <th className="d-none d-lg-table-cell sort" scope="col"></th>
          <th className="column-action" scope="col" />
        </tr>
      </thead>
      <tbody>
        {Array.from({ length: pageSize }, (_, i) => i).map((item, i) => {
          return (
            <tr key={i}>
              <td className="select-disabled">
                <label className="custom-checkbox">
                  <input type="checkbox" disabled={true} />
                  <span className="checkmark" />
                </label>
              </td>
              <td />
              <td>
                <span
                  className="sceleton-loader sceleton-p"
                  style={{ width: "110px" }}
                />
              </td>
              <td className="d-none d-xl-table-cell">
                <span className="sceleton-loader sceleton-p" />
              </td>
              <td className="text-right">
                <span className="sceleton-loader sceleton-p" />
              </td>
              <td className="d-none d-lg-table-cell modified-date">
                <span className="sceleton-loader sceleton-p" />
              </td>
              <td className="text-right select-disabled">
                <span className="sceleton-loader sceleton-p" />
              </td>
            </tr>
          );
        })}
      </tbody>
    </table>
  );
};

export default SceletonGridContainer;
