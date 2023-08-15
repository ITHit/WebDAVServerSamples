import { ITHit } from "webdav.client";
export class OpenItemsCollectionResult {
  public uploadItem: ITHit.WebDAV.Client.Upload.UploadItem;
  public asyncResult: ITHit.WebDAV.Client.AsyncResult;
  constructor(
    uploadItem: ITHit.WebDAV.Client.Upload.UploadItem,
    asyncResult: ITHit.WebDAV.Client.AsyncResult
  ) {
    this.asyncResult = asyncResult;
    this.uploadItem = uploadItem;
  }
}
