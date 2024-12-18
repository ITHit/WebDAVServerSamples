import React, { useEffect, useRef } from "react";
import { useAppDispatch, useAppSelector } from "../../../app/hooks/common";
import { appendNewPage, getItems } from "../gridSlice";
import GridRow from "./GridRow";

const GridBody: React.FC = () => {
  const items = useAppSelector(getItems);
  const countPages = useAppSelector((state) => state.grid.countPages);
  const countPagesLoaded = useAppSelector(
    (state) => state.grid.countPagesLoaded
  );
  const dispatch = useAppDispatch();
  const observerRef = useRef<HTMLTableRowElement | null>(null);
  const sceletonRowsCount = 3;
  const loadMore = countPages > 0 && countPages != countPagesLoaded;

  // Fetch more items when the user scrolls to the bottom of the page
  useEffect(() => {
    if (!loadMore) return;

    const currentRef = observerRef.current;
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting) {
          dispatch(appendNewPage()); // Action to fetch more items
        }
      },
      { threshold: 1.0 }
    );

    if (currentRef) {
      observer.observe(currentRef);
    }

    return () => {
      if (currentRef) observer.unobserve(currentRef);
    };
  }, [dispatch, loadMore]);

  return (
    <tbody>
      {items.map((item, i) => {
        return <GridRow item={item} index={i} key={"item-" + i} />;
      })}
      {loadMore && (
        <>
          <tr ref={observerRef}>
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
          {Array.from({ length: sceletonRowsCount }, (_, i) => i).map(
            (_item, i) => {
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
            }
          )}
          <tr>
            <td colSpan={100} className="load-more-block">
              <div className="loader"></div>
            </td>
          </tr>
        </>
      )}
    </tbody>
  );
};

export default GridBody;
