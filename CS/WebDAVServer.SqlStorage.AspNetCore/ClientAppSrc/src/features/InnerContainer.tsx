import React, { useCallback, useRef } from "react";
import GridContainer from "./grid/components/GridContainer";
import Pagination from "./grid/components/Pagination";
import Toolbar from "./toolbar/Toolbar";
import Breadcrumb from "./Breadcrumb";
import InstallProtocolModal from "./modals/InstallProtocolModal";
import ErrorModal from "./modals/ErrorModal";
import RewriteModal from "./modals/RewriteModal";
import Search from "./search/Search";
import { getIsDragging, setIsDragging } from "./upload/uploadSlice";
import { useAppDispatch, useAppSelector } from "../app/hooks/common";
import Uploader from "./upload/Uploader";

type Props = {};
const InnerContainer: React.FC<Props> = () => {
  const inputRef = useRef(null);
  const dispatch = useAppDispatch();
  let dropCounter = 0;
  const isDragging = useAppSelector(getIsDragging);

  const handleDragEnter = useCallback(
    (e: React.DragEvent<HTMLDivElement>) => {
      dropCounter++;
      dispatch(setIsDragging(true));
    },
    [dropCounter, dispatch]
  );
  const handleDragLeave = useCallback(
    (e: React.DragEvent<HTMLDivElement>) => {
      dropCounter--;
      if (dropCounter <= 0) {
        // eslint-disable-next-line react-hooks/exhaustive-deps
        dropCounter = 0;
        dispatch(setIsDragging(false));
      }
    },
    [dropCounter, dispatch]
  );
  const handleDrop = useCallback(
    (e: React.DragEvent<HTMLDivElement>) => {
      // eslint-disable-next-line react-hooks/exhaustive-deps
      dropCounter = 0;
      dispatch(setIsDragging(false));
    },
    [dropCounter, dispatch]
  );

  return (
    <div
      id="ithit-dropzone"
      onDragEnterCapture={handleDragEnter}
      onDragLeave={handleDragLeave}
      onDropCapture={handleDrop}
      className={isDragging ? "dropzone" : ""}
    >
      <div className="fixed-controls">
        <Breadcrumb isSearchMode={false} />
        <Search />
        <Toolbar />
      </div>
      <div>
        <Uploader />
        <GridContainer />
        <Pagination />
      </div>
      <InstallProtocolModal />
      <ErrorModal />
      <RewriteModal />
      <input
        id="ithit-hidden-input"
        className="d-none"
        type="file"
        multiple
        ref={inputRef}
      />
    </div>
  );
};

export default InnerContainer;
