import { useEffect } from "react";
import MainContainer from "./features/MainContainer";

import { StoreWorker } from "./app/storeWorker";
import { useLocation } from "react-router-dom";
import { UrlResolveService } from "./services/UrlResolveService";
import { UploadService } from "./services/UploadService";
import queryString from "query-string";
import { IQueryParams, QueryParams } from "./models/QueryParams";
import { useWebSocket } from "./app/hooks/useWebSocket";

function App() {
  const location = useLocation();
  const query = queryString.parse(location.search);
  const queryParams = new QueryParams(query as IQueryParams);
  // Initialize WebSocket connection
  useWebSocket();

  const currentUrl =
    UrlResolveService.getRootUrl() +
    UrlResolveService.getTail(UrlResolveService.getOrigin() + location.pathname, UrlResolveService.getRootUrl());

  useEffect(() => {
    StoreWorker.refresh(currentUrl, queryParams);
  });

  useEffect(() => {
    UploadService.addDropzone();

    return () => {
      UploadService.destroy();
    };
  }, []);

  return <MainContainer />;
}

export default App;
