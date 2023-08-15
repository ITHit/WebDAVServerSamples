import React from "react";
import { useAppSelector, useAppDispatch } from "../../../app/hooks/common";
import { StoreWorker } from "../../../app/storeWorker";

import { getCountPages, getCurrentPage, setCurrentPage } from "../gridSlice";
type Props = {};

const Pagination: React.FC<Props> = () => {
  const dispatch = useAppDispatch();
  const countPages = useAppSelector(getCountPages);
  const currentPage = useAppSelector(getCurrentPage);
  const paginationItems = (function () {
    const pItems = [];
    for (let i = 1; i <= countPages; i++) {
      pItems.push(i);
    }
    return pItems;
  })();
  const handleClick = (page: number) => {
    dispatch(setCurrentPage(page));
    StoreWorker.refresh();
  };
  return (
    <div>
      {countPages > 1 && (
        <div className="row">
          <div className="col-12 align-items-end">
            <nav aria-label="Page navigation">
              <ul className="pagination flex-wrap justify-content-end ithit-pagination-container">
                <li
                  className={`page-item ${currentPage === 1 ? "disabled" : ""}`}
                >
                  <button className="page-link" onClick={() => handleClick(1)}>
                    &lt;&lt;
                  </button>
                </li>
                {paginationItems.map((item, i) => {
                  return (
                    <li
                      key={"pagination" + i}
                      className={`page-item ${
                        currentPage === item ? "active" : ""
                      }`}
                    >
                      <button
                        className={`page-link ${
                          currentPage === item ? "disabled" : ""
                        }`}
                        disabled={currentPage === item}
                        onClick={() => handleClick(item)}
                      >
                        {item}
                      </button>
                    </li>
                  );
                })}

                <li
                  className={`page-item ${
                    currentPage === countPages ? "disabled" : ""
                  }`}
                >
                  <button
                    className="page-link"
                    onClick={() => handleClick(countPages)}
                  >
                    &gt;&gt;
                  </button>
                </li>
              </ul>
            </nav>
          </div>
        </div>
      )}
    </div>
  );
};

export default Pagination;
