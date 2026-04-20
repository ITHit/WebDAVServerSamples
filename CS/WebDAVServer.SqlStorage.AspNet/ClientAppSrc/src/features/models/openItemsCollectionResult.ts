import type { IUploadItem } from "./uploadItemRow";

export interface IAsyncCallbackResult {
  IsSuccess: boolean;
  Status: { Code: number };
  Error: unknown;
  Result: unknown;
}

export interface OpenItemsCollectionResult {
  uploadItem: IUploadItem;
  asyncResult: IAsyncCallbackResult;
}
