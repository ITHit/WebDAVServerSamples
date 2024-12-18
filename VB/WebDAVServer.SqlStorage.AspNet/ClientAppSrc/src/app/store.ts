import { configureStore, ThunkAction, Action } from "@reduxjs/toolkit";
import rootReducer from "./rootReducer";
import { createBrowserHistory } from "history";
import { options } from "./storeMiddleware";

export const history = createBrowserHistory();

export const store = configureStore({
  reducer: rootReducer,
  middleware: (getDefaultMiddleware) => getDefaultMiddleware(options),
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;
export type AppThunk<ReturnType = void> = ThunkAction<
  ReturnType,
  RootState,
  unknown,
  Action<string>
>;
