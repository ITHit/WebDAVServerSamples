import { ITHit } from "webdav.client";
import { UrlResolveService } from "../services/UrlResolveService";
import { store } from "../app/store";
import { GridState, setError } from "../features/grid/gridSlice";
import { UploadItemRow } from "../models/UploadItemRow";
import { OpenItemsCollectionResult } from "../models/OpenItemsCollectionResult";
import { CommonService } from "./CommonService";
import { WebDavService } from "./WebDavService";
import { WebDavError } from "../models/WebDavError";
import { getI18n } from "react-i18next";
import { RewriteItemsData } from "../models/RewriteItemsData";
import {
  addUploadItemRow,
  removeUploadItemRow,
  setRewriteItemsData,
} from "../features/upload/uploadSlice";
const i18n = getI18n();
const { grid } = store.getState() as { grid: GridState };

class UploadItemsService {
  private uploader: ITHit.WebDAV.Client.Upload.Uploader | null = null;

  public initUploader() {
    if (this.uploader == null) {
      this.uploader = new ITHit.WebDAV.Client.Upload.Uploader();
      const { currentUrl } = grid;
      this.setUploadUrl(currentUrl);
      this.uploader.Queue.AddListener(
        "OnQueueChanged",
        this.queueChanged,
        this
      );
      this.uploader.Queue.AddListener(
        "OnUploadItemsCreated",
        this.onUploadItemsCreated,
        this
      );
    }
  }

  public destroy() {
    if (this.uploader !== null) {
      this.uploader.Queue.RemoveListener(
        "OnQueueChanged",
        this.queueChanged,
        this
      );
      this.uploader.Queue.RemoveListener(
        "OnUploadItemsCreated",
        this.onUploadItemsCreated,
        this
      );
    }
  }

  public setUploadUrl(url: string) {
    this.uploader?.SetUploadUrl(UrlResolveService.decode(url));
  }

  public addDropzone() {
    this.initUploader();
    this.uploader?.Inputs.AddById("ithit-hidden-input");
    this.uploader?.DropZones.AddById("ithit-dropzone");
  }

  public addInput(inputId: string) {
    this.initUploader();
    this.uploader?.Inputs.AddById(inputId);
  }

  private queueChanged(e: ITHit.WebDAV.Client.Upload.Events.QueueChanged) {
    // Display each item added to the upload queue in the grid.
    e.AddedItems.forEach(function (value) {
      store.dispatch(addUploadItemRow(new UploadItemRow(value)));
    });

    e.RemovedItems.forEach(function (value) {
      store.dispatch(removeUploadItemRow(value));
    });
  }

  private onUploadItemsCreated(
    e: ITHit.WebDAV.Client.Upload.Events.UploadItemsCreated
  ) {
    /* Validate file extensions, size, name, etc. here. */
    let oValidationError = this.validateUploadItems(e.Items);
    if (oValidationError) {
      store.dispatch(setError(oValidationError));
      return;
    }

    /* Below we will check if each file exists on the server 
          and ask a user if files should be overwritten or skipped. */
    this.getExistsAsync(e.Items, function (oAsyncResult) {
      if (oAsyncResult.IsSuccess && oAsyncResult.Result.length === 0) {
        // No items exists on the server.
        // Add all items to the upload queue.
        e.Upload(e.Items);
        return;
      }
      if (!oAsyncResult.IsSuccess) {
        // Some error occurred during item existence verification requests.
        // Show error dialog with error description.
        // Mark all items as failed and add to the upload list.
        store.dispatch(
          setError(
            new WebDavError(
              i18n.t("phrases.errors.failedCheckExistsErrorMessage"),
              oAsyncResult.Error
            )
          )
        );

        e.Items.forEach(function (oUploadItem) {
          // Move an item into the error state.
          // Upload of this item will NOT start when added to the queue.
          if (
            oAsyncResult.Error instanceof
            ITHit.WebDAV.Client.Exceptions.WebDavException
          )
            oUploadItem.SetFailed(oAsyncResult.Error);
        });

        // Add all items to the upload queue, so a user can start the upload later.
        e.Upload(e.Items);

        return;
      }

      let sItemsList = ""; // List of items to be displayed in Overwrite / Skip / Cancel dialog.

      let aExistsUploadItems: ITHit.WebDAV.Client.Upload.UploadItem[] = [];
      oAsyncResult.Result.forEach(function (
        oUploadItem: ITHit.WebDAV.Client.Upload.UploadItem
      ) {
        // For the sake of simplicity folders are never deleted when upload canceled.
        if (!oUploadItem.IsFolder()) {
          // File exists so we should not delete it when file's upload canceled.
          oUploadItem.SetDeleteOnCancel(false);
        }

        // Mark item as verified to avoid additional file existence verification requests.
        (oUploadItem.CustomData as any).FileExistanceVerified = true;

        sItemsList += oUploadItem.GetRelativePath() + "<br/>";
        aExistsUploadItems.push(oUploadItem);
      });
      let onOverwrite = function () {
        // Mark all items that exist on the server with overwrite flag.
        aExistsUploadItems.forEach(function (oUploadItem) {
          if (oUploadItem.IsFolder()) return;

          // The file will be overwritten if it exists on the server.
          oUploadItem.SetOverwrite(true);
        });

        // Add all items to the upload queue.
        e.Upload(e.Items);
      };

      let onSkipExists = function () {
        // Create list of items that do not exist on the server.
        /** @type {ITHit.WebDAV.Client.Upload.UploadItem[]} aNotExistsUploadItems */
        let grep = function (
          elems: ITHit.WebDAV.Client.Upload.UploadItem[],
          callback: (arg: ITHit.WebDAV.Client.Upload.UploadItem) => boolean,
          invert: boolean
        ) {
          let callbackInverse,
            matches = [],
            i = 0,
            length = elems.length,
            callbackExpect = !invert;

          // Go through the array, only saving the items
          // that pass the validator function
          for (; i < length; i++) {
            callbackInverse = !callback(elems[i]);
            if (callbackInverse !== callbackExpect) {
              matches.push(elems[i]);
            }
          }

          return matches;
        };
        let aNotExistsUploadItems = grep(
          e.Items,
          function (oUploadItem) {
            return aExistsUploadItems.indexOf(oUploadItem) >= 0;
          },
          false
        );

        // Add only items that do not exist on the server to the upload queue.
        e.Upload(aNotExistsUploadItems);
      };
      /* One or more items exists on the server. Show Overwrite / Skip / Cancel dialog.*/
      let rewriteData = new RewriteItemsData(
        /* A user selected to overwrite existing files. */
        onOverwrite,
        /* A user selected to skip existing files. */
        onSkipExists,
        sItemsList
      );
      store.dispatch(setRewriteItemsData(rewriteData));
    });
  }

  private validateUploadItems(
    aUploadItems: ITHit.WebDAV.Client.Upload.UploadItem[]
  ) {
    for (let i = 0; i < aUploadItems.length; i++) {
      let oValidationError = this.validateName(aUploadItems[i]);
      if (oValidationError) {
        return oValidationError;
      }
    }
  }

  private validateName(oUploadItem: ITHit.WebDAV.Client.Upload.UploadItem) {
    let sValidationMessage = CommonService.validateName(oUploadItem.GetName());
    if (sValidationMessage) {
      return new WebDavError(
        sValidationMessage + "\nUri:" + oUploadItem.GetUrl(),
        null
      );
    }
  }

  private getExistsAsync(
    aUploadItems: ITHit.WebDAV.Client.Upload.UploadItem[],
    fCallback: (asyncResult: ITHit.WebDAV.Client.AsyncResult) => void
  ) {
    this.openItemsCollectionAsync(
      aUploadItems,
      function (aResultCollection: OpenItemsCollectionResult[]) {
        let oFailedResult = aResultCollection.find(
          (el) =>
            !(el.asyncResult.IsSuccess || el.asyncResult.Status.Code === 404)
        );

        if (oFailedResult) {
          fCallback(oFailedResult.asyncResult);
          return;
        }

        let aExistsItems = aResultCollection
          .filter(function (oResult: OpenItemsCollectionResult) {
            return oResult.asyncResult.IsSuccess;
          })
          .map(function (oResult) {
            return oResult.uploadItem;
          });

        const createAsyncResult = function (oResult: any) {
          let result = {
            Result: oResult,
            IsSuccess: true,
            Error: null,
            Status: ITHit.WebDAV.Client.HttpStatus.None,
          } as ITHit.WebDAV.Client.AsyncResult;

          return result;
        };
        fCallback(createAsyncResult(aExistsItems));
      }
    );
  }

  private openItemsCollectionAsync(
    aUploadItems: ITHit.WebDAV.Client.Upload.UploadItem[],
    fCallback: (arg: OpenItemsCollectionResult[]) => void
  ) {
    let iCounter = aUploadItems.length;
    let aResults: OpenItemsCollectionResult[] = [];
    if (iCounter === 0) {
      fCallback(aResults);
      return;
    }

    aUploadItems.forEach(function (oUploadItem) {
      WebDavService.openItem(
        UrlResolveService.encodeUri(oUploadItem.GetUrl()),
        function (oAsyncResult: ITHit.WebDAV.Client.AsyncResult) {
          iCounter--;
          aResults.push({
            uploadItem: oUploadItem,
            asyncResult: oAsyncResult,
          });

          if (iCounter === 0) {
            fCallback(aResults);
          }
        }
      );
    });
  }

  private static getGrid() {
    const { grid } = store.getState() as { grid: GridState };
    return grid;
  }
}
export const UploadService = new UploadItemsService();
