import { combineReducers } from "redux";
import { connectRouter } from "connected-react-router";
import { History } from "history";

import grid from "../features/grid/gridSlice";
import upload from "../features/upload/uploadSlice";

const rootReducer = (history: History) =>
  combineReducers({
    router: connectRouter(history),
    grid,
    upload,
  });

export default rootReducer;
