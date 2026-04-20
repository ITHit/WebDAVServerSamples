import { IUploadItem, UploadItemRow } from "./uploadItemRow";
import { RewriteItemsData } from "./rewriteItemsData";
import { UploadState, fromITHitState } from "@/domain/value-objects/UploadState";
import { shouldUploadImmediately, startUploadRetryTimer } from "./uploadCore";
import { UploadEventPort } from "./uploadEventPort";
import { UploadInfrastructurePort, UploadErrorFactoryPort } from './uploadInfrastructurePort';
import { UploadUiPort } from './uploadUiPort';

interface ProgressChangedEvent {
  Sender: IUploadItem;
}

interface StateChangedEvent {
  NewState: string;
}

interface BeforeUploadStartedEvent {
  Sender: IUploadItem;
  Upload(): void;
}

interface UploadErrorEvent {
  Skip(): void;
  Retry(): void;
}

export interface UploadItemRowHandlers {
  onProgress: (e: ProgressChangedEvent) => void;
  onStateChanged: (e: StateChangedEvent) => void;
  onBeforeUploadStarted: (e: BeforeUploadStartedEvent) => void;
  onUploadError: (e: UploadErrorEvent) => void;
}

export function createUploadItemHandlers(
  row: UploadItemRow,
  upload: UploadUiPort,
  events: UploadEventPort,
  infrastructure: UploadInfrastructurePort,
  errors: Pick<UploadErrorFactoryPort, 'createExistsCheckError'>
) {
  const handlers: UploadItemRowHandlers = {
    onProgress: (e: ProgressChangedEvent) => {
      row.currentState = fromITHitState(e.Sender.GetState());
    },
    onStateChanged: (e: StateChangedEvent) => {
      row.currentState = fromITHitState(e.NewState);
      if (row.currentState === UploadState.Completed) {
        events.onFolderRefreshRequested();
      }
    },
    onBeforeUploadStarted: (e: BeforeUploadStartedEvent) => {
      const oItem = e.Sender;
      const sHref = infrastructure.encodeUri(oItem.GetUrl());
      if (shouldUploadImmediately(oItem)) {
        e.Upload();
        return;
      }
      infrastructure.openItemCallback(sHref, (oAsyncResult) => {
        if (!oAsyncResult.IsSuccess && oAsyncResult.Status.Code === 404) {
          e.Upload();
          return;
        }

        if (!oAsyncResult.IsSuccess) {
          events.onErrorOccurred(errors.createExistsCheckError(oAsyncResult.Error));
          oItem.SetFailed(oAsyncResult.Error);
          return;
        }

        const rewriteData = new RewriteItemsData(
          /* A user selected to overwrite existing file. */
          function () {
            // Do not delete item if upload canceled (it existed before the upload).
            oItem.SetDeleteOnCancel(false);

            // The item will be overwritten if it exists on the server.
            oItem.SetOverwrite(true);

            // All async requests completed - start upload.
            e.Upload();
          },
          /* A user selected to skip existing files. */
          function () { },
          [oItem.GetRelativePath()]
        );
        upload.setRewriteItemsData(rewriteData);
      });
    },
    onUploadError: (e: UploadErrorEvent) => {
      startUploadRetryTimer(
        {
          currentRetry: row.currentRetry,
          maxRetry: row.maxRetry,
          retryDelaySeconds: row.retryDelay,
        },
        {
          setRetryMessage: (timeLeftMs) => row.setRetryMessage(timeLeftMs),
          removeRetryMessage: () => row.removeRetryMessage(),
          incrementRetry: () => {
            row.currentRetry++;
          },
          onRetry: () => {
            // Request number of bytes successfully saved on the server
            // and retry upload from next byte.
            e.Retry();
          },
          onSkip: () => e.Skip(),
          setCancelRetryCallback: (callback) => {
            row.cancelRetryCallback = callback;
          },
        }
      );
    }
  };

  return handlers;
}