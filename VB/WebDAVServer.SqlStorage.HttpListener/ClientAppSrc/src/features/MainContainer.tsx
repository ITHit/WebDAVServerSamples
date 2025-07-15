import React, { useEffect } from "react";
import InnerContainer from "./InnerContainer";
import Header from "./Header";
import { useAppDispatch, useAppSelector } from "../app/hooks/common";
import { getForceRedirectUrl, setForceRedirectUrl } from "./grid/gridSlice";
import { StoreWorker } from "../app/storeWorker";

const MainContainer: React.FC = () => {
  const forceRedirectUrl = useAppSelector(getForceRedirectUrl);
  const dispatch = useAppDispatch();
  useEffect(() => {
    if (forceRedirectUrl) {
      StoreWorker.refresh(forceRedirectUrl);
      dispatch(setForceRedirectUrl(""));
    }
  }, [dispatch, forceRedirectUrl]);

  return (
    <>
      <Header />
      <div className="container-fluid">
        <InnerContainer />
      </div>
    </>
  );
};

export default MainContainer;
