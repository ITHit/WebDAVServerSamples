import { FormatUtils } from "@/shared/utils/formatUtils";
import { UploadItemRowHandlers } from "./uploadItemHandlers";
import { UploadState, fromITHitState } from "@/domain/value-objects/UploadState";

export interface IUploadItem {
  GetState(): string;
  GetUrl(): string;
  GetName(): string;
  GetProgress(): { TotalBytes: number; UploadedBytes: number; Completed: number; Speed: number; RemainingTime: number };
  IsFolder(): boolean;
  GetRelativePath(): string;
  GetOverwrite(): boolean;
  SetFailed(error: unknown): void;
  SetDeleteOnCancel(value: boolean): void;
  SetOverwrite(value: boolean): void;
  CustomData: unknown;
  AddListener(event: string, callback: (event: never) => void): void;
  RemoveListener(event: string, callback: (event: never) => void): void;
  CancelAsync(): void;
  PauseAsync(callback?: () => void): void;
  StartAsync(callback?: () => void): void;
}

export class UploadItemRow {
  public currentState: UploadState = UploadState.Queued;
  public retryMessage: string = "";
  public uploadItem: IUploadItem;
  public handlers: UploadItemRowHandlers | null;

  public maxRetry: number = 10;
  public currentRetry: number = 0;
  public retryDelay: number = 10;
  public cancelRetryCallback: (() => void) | null = null;

  constructor(uploadItem: IUploadItem) {
    this.uploadItem = uploadItem;
    this.currentState = fromITHitState(this.uploadItem.GetState());
    this.handlers = null;
  }

  addHandlers(handlers: UploadItemRowHandlers) {
    this.handlers = handlers;
    this.uploadItem.AddListener("OnProgressChanged", handlers.onProgress);
    this.uploadItem.AddListener("OnStateChanged", handlers.onStateChanged);
    this.uploadItem.AddListener("OnBeforeUploadStarted", handlers.onBeforeUploadStarted);
    this.uploadItem.AddListener("OnUploadError", handlers.onUploadError);
  }

  destroy() {
    if (this.handlers !== null) {
      this.uploadItem.RemoveListener(
        "OnProgressChanged",
        this.handlers.onProgress
      );
      this.uploadItem.RemoveListener(
        "OnStateChanged",
        this.handlers.onStateChanged
      );
      this.uploadItem.RemoveListener(
        "OnBeforeUploadStarted",
        this.handlers.onBeforeUploadStarted
      );
      this.uploadItem.RemoveListener(
        "OnUploadError",
        this.handlers.onUploadError
      );
    }
  }

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

  setRetryMessage(timeLeft: number) {
    const sRetryMessageFormat = "Retry in: {0}";
    this.retryMessage = FormatUtils.pasteFormat(
      sRetryMessageFormat,
      FormatUtils.timeSpan(Math.ceil(timeLeft / 1000))
    );
  }

  removeRetryMessage() {
    this.retryMessage = "";
  }

  cancelRetry() {
    if (this.cancelRetryCallback) this.cancelRetryCallback.call(this);
  }
}
