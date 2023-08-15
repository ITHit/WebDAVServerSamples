import React, { useEffect } from "react";
import MainContainer from "./features/MainContainer";

import { StoreWorker } from "./app/storeWorker";
import { useLocation } from "react-router-dom";
import { UrlResolveService } from "./services/UrlResolveService";
import { UploadService } from "./services/UploadService";
import { parse } from "query-string";
import { QueryParams } from "./models/QueryParams";

function App() {
  const location = useLocation();
  const query = parse(location.search);
  const queryParams = new QueryParams(query);

  const currentUrl =
    UrlResolveService.getRootUrl() +
    UrlResolveService.getTail(
      UrlResolveService.getOrigin() + location.pathname,
      UrlResolveService.getRootUrl()
    );

  useEffect(() => {
    StoreWorker.refresh(currentUrl, queryParams);
  });

  useEffect(() => {
    UploadService.addDropzone();

    return function cleanup() {
      UploadService.destroy();
    };
  });
  return <MainContainer />;
}

export default App;
