import React, { useCallback, useRef } from "react";
import GridContainer from "./grid/components/GridContainer";
import Toolbar from "./toolbar/Toolbar";
import Breadcrumb from "./Breadcrumb";
import InstallProtocolModal from "./modals/InstallProtocolModal";
import ErrorModal from "./modals/ErrorModal";
import RewriteModal from "./modals/RewriteModal";
import Search from "./search/Search";
import { getIsDragging, setIsDragging } from "./upload/uploadSlice";
import { useAppDispatch, useAppSelector } from "../app/hooks/common";
import Uploader from "./upload/Uploader";

const InnerContainer: React.FC = () => {
  const inputRef = useRef(null);
  const dispatch = useAppDispatch();
  const dropCounter = useRef(0);
  const isDragging = useAppSelector(getIsDragging);

  const handleDragEnter = useCallback(() => {
    dropCounter.current++;
    dispatch(setIsDragging(true));
  }, [dispatch]);

  const handleDragLeave = useCallback(() => {
    dropCounter.current--;
    if (dropCounter.current <= 0) {
      dropCounter.current = 0;
      dispatch(setIsDragging(false));
    }
  }, [dispatch]);

  const handleDrop = useCallback(() => {
    dropCounter.current = 0;
    dispatch(setIsDragging(false));
  }, [dispatch]);

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
      </div>
      <Toolbar />
      <Uploader />
      <GridContainer />
      <InstallProtocolModal />
      <ErrorModal />
      <RewriteModal />
      <input id="ithit-hidden-input" className="d-none" type="file" multiple ref={inputRef} />
    </div>
  );
};

export default InnerContainer;
