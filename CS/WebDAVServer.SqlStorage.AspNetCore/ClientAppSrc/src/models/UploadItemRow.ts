import { ITHit } from "webdav.client";
import { store } from "../app/store";
import { UrlResolveService } from "../services/UrlResolveService";
import { WebDavService } from "../services/WebDavService";
import { WebDavError } from "./WebDavError";
import { getI18n } from "react-i18next";
import { setError } from "../features/grid/gridSlice";
import { RewriteItemsData } from "./RewriteItemsData";
import { setRewriteItemsData } from "../features/upload/uploadSlice";
import { StoreWorker } from "../app/storeWorker";
import { CommonService } from "../services/CommonService";
const i18n = getI18n();

export class UploadItemRow {
  public currentState: string = "";
  public retryMessage: string = "";
  public uploadItem: ITHit.WebDAV.Client.Upload.UploadItem;

  private maxRetry: number = 10;
  private currentRetry: number = 0;
  private retryDelay: number = 10;
  private cancelRetryCallback: Function | null = null;

  constructor(uploadItem: ITHit.WebDAV.Client.Upload.UploadItem) {
    this.uploadItem = uploadItem;
    this.uploadItem.AddListener("OnProgressChanged", this.onProgress);
    this.uploadItem.AddListener("OnStateChanged", this.onStateChanged);
    this.uploadItem.AddListener(
      "OnBeforeUploadStarted",
      this.onBeforeUploadStarted
    );
    this.uploadItem.AddListener("OnUploadError", this.onUploadError);

    this.currentState = this.uploadItem.GetState();
  }

  public destroy() {
    this.uploadItem.RemoveListener("OnProgressChanged", this.onProgress);
    this.uploadItem.RemoveListener("OnStateChanged", this.onStateChanged);
    this.uploadItem.RemoveListener(
      "OnBeforeUploadStarted",
      this.onBeforeUploadStarted
    );
    this.uploadItem.RemoveListener("OnUploadError", this.onUploadError);
  }

  private onProgress = (
    e: ITHit.WebDAV.Client.Upload.Events.ProgressChanged
  ) => {
    this.currentState = (e.Sender as any).GetState();
  };

  private onStateChanged = (
    e: ITHit.WebDAV.Client.Upload.Events.StateChanged
  ) => {
    this.currentState = e.NewState;
    if (e.NewState === ITHit.WebDAV.Client.Upload.State.Completed) {
      StoreWorker.refresh();
    }
  };

  private onBeforeUploadStarted = (
    e: ITHit.WebDAV.Client.Upload.Events.BeforeUploadStarted
  ) => {
    var oItem = e.Sender as ITHit.WebDAV.Client.Upload.UploadItem;
    var sHref = UrlResolveService.encodeUri(oItem.GetUrl());
    if (
      oItem.GetOverwrite() ||
      oItem.IsFolder() ||
      (oItem.CustomData as any).FileExistanceVerified
    ) {
      e.Upload();
      return;
    }
    WebDavService.openItem(sHref, function (oAsyncResult) {
      if (!oAsyncResult.IsSuccess && oAsyncResult.Status.Code === 404) {
        // The file does not exist on the server, start the upload.
        e.Upload();
        return;
      }

      if (!oAsyncResult.IsSuccess) {
        store.dispatch(
          setError(
            new WebDavError(
              i18n.t("phrases.errors.failedCheckExistsErrorMessage"),
              oAsyncResult.Error
            )
          )
        );

        (e.Sender as any).SetFailed(oAsyncResult.Error);
        return;
      }

      var rewriteData = new RewriteItemsData(
        /* A user selected to overwrite existing file. */
        function () {
          // Do not delete item if upload canceled (it existed before the upload).
          (e.Sender as any).SetDeleteOnCancel(false);

          // The item will be overwritten if it exists on the server.
          (e.Sender as any).SetOverwrite(true);

          // All async requests completed - start upload.
          e.Upload();
        },
        /* A user selected to skip existing files. */
        function () {},
        oItem.GetRelativePath()
      );

      store.dispatch(setRewriteItemsData(rewriteData));
    });
  };

  cancelClickHandler() {
    this.cancelRetry();
    this.uploadItem.CancelAsync();
  }
  pauseClickHandler(fCallback?: () => void) {
    this.cancelRetry();
    this.uploadItem.PauseAsync(fCallback);
  }
  playClickHandler(fCallback?: () => void) {
    this.currentRetry = 0;
    this.uploadItem.StartAsync(fCallback);
  }

  private setRetryMessage(timeLeft: number) {
    var sRetryMessageFormat = "Retry in: {0}";
    this.retryMessage = CommonService.pasteFormat(
      sRetryMessageFormat,
      CommonService.timeSpan(Math.ceil(timeLeft / 1000))
    );
  }

  private removeRetryMessage() {
    this.retryMessage = "";
  }

  private cancelRetry() {
    if (this.cancelRetryCallback) this.cancelRetryCallback.call(this);
  }

  private onUploadError = (
    error: ITHit.WebDAV.Client.Upload.Events.UploadError
  ) => {
    // Here you can verify error code returned by the server and show error UI,
    // for example if server-side validation failed.

    // Stop upload if max upload retries reached.
    if (this.maxRetry <= this.currentRetry) {
      //this._ShowError(oUploadError.Error);
      error.Skip();
      return;
    }
    var me = this;
    // Retry upload.
    var retryTime = new Date().getTime() + this.retryDelay * 1000;
    var retryTimerId = setInterval(function () {
      var timeLeft = retryTime - new Date().getTime();
      if (timeLeft > 0) {
        me.setRetryMessage(timeLeft);
        return;
      }
      clearInterval(retryTimerId);
      me.currentRetry++;
      me.removeRetryMessage();

      // Request number of bytes succesefully saved on the server
      // and retry upload from next byte.
      error.Retry();
    }, 1000);

    this.cancelRetryCallback = function () {
      clearInterval(retryTimerId);
      this.removeRetryMessage();
    };
  };
}
