import { createSlice, PayloadAction } from "@reduxjs/toolkit";
import { ITHit } from "webdav.client";
import { RootState } from "../../app/store";
import { getMidIgnoredPath } from "../../app/storeFunctions";
import { RewriteItemsData } from "../../models/RewriteItemsData";
import { UploadItemRow } from "../../models/UploadItemRow";
import { CommonService } from "../../services/CommonService";

export interface UploadState {
  uploadItemRows: UploadItemRow[];
  rewriteItemsData: RewriteItemsData | null;
  isDragging: boolean;
}

const initialState: UploadState = {
  uploadItemRows: [],
  rewriteItemsData: null,
  isDragging: false,
};

export const uploadSlice = createSlice({
  name: "upload",
  initialState,
  reducers: {
    addUploadItemRow: (state, action: PayloadAction<UploadItemRow>) => {
      state.uploadItemRows.push(action.payload);
    },
    removeUploadItemRow: (
      state,
      action: PayloadAction<ITHit.WebDAV.Client.Upload.UploadItem>
    ) => {
      state.uploadItemRows = state.uploadItemRows.filter((item) => {
        if (item.uploadItem === action.payload) {
          item.destroy();
        }
        return item.uploadItem === action.payload;
      });
    },
    setRewriteItemsData: (
      state,
      action: PayloadAction<RewriteItemsData | null>
    ) => {
      state.rewriteItemsData = action.payload;
    },
    setIsDragging: (state, action: PayloadAction<boolean>) => {
      state.isDragging = action.payload;
    },
  },
});

export const {
  addUploadItemRow,
  removeUploadItemRow,
  setRewriteItemsData,
  setIsDragging,
} = uploadSlice.actions;

export const getUploadItemRows = (state: RootState) =>
  state.upload.uploadItemRows;
export const getRewriteItemsData = (state: RootState) =>
  state.upload.rewriteItemsData;
export const getIsDragging = (state: RootState) => state.upload.isDragging;

export const uploadMidIgnoredActions = [
  addUploadItemRow.toString(),
  removeUploadItemRow.toString(),
  setRewriteItemsData.toString(),
];

export const uploadMidIgnoredPaths = [
  getMidIgnoredPath(
    uploadSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.uploadItemRows)
  ),
  getMidIgnoredPath(
    uploadSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.rewriteItemsData)
  ),
];

export default uploadSlice.reducer;
