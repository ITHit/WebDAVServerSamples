import { combineReducers } from "redux";

import grid from "../features/grid/gridSlice";
import upload from "../features/upload/uploadSlice";

const rootReducer = combineReducers({
  grid,
  upload,
});

export default rootReducer;
