import React from "react";
import GridBody from "./GridBody";
import GridHeader from "./GridHeader";
import { useAppSelector } from "../../../app/hooks/common";
import { getLoading } from "../gridSlice";
import SceletonGridContainer from "./SceletonGridContainer";
import { useTranslation } from "react-i18next";

const GridContainer: React.FC = () => {
  const { t } = useTranslation();
  const loading = useAppSelector(getLoading);

  return (
    <div className="ithit-grid-wrapper">
      <div className="drop-files-header">
        <div className="drop-files-title">
          <i className="icon icon-upload-items"></i>
          {t("phrases.grid.dragFiles")}
        </div>
      </div>
      <div className="table-responsive">
        {loading && <SceletonGridContainer />}
        {!loading && (
          <table className="table table-hover ithit-grid-container">
            <GridHeader />
            <GridBody />
          </table>
        )}
      </div>
    </div>
  );
};

export default GridContainer;
