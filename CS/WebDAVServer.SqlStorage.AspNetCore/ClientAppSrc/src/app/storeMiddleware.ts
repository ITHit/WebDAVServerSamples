import {
  gridMidIgnoredActions,
  gridMidIgnoredPaths,
} from "../features/grid/gridSlice";
import {
  uploadMidIgnoredActions,
  uploadMidIgnoredPaths,
} from "../features/upload/uploadSlice";

export const options = {
  serializableCheck: {
    // Ignore these action types
    ignoredActions: [...gridMidIgnoredActions, ...uploadMidIgnoredActions],
    // Ignore these field paths in all actions
    ignoredActionPaths: [],
    // Ignore these paths in the state
    ignoredPaths: [...gridMidIgnoredPaths, ...uploadMidIgnoredPaths],
  },
};
