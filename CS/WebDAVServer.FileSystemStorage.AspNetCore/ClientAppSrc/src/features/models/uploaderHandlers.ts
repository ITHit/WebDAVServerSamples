import { IUploadItem, UploadItemRow } from "./uploadItemRow";
import { createUploadItemHandlers } from "./uploadItemHandlers";
import { RewriteItemsData } from "./rewriteItemsData";
import { OpenItemsCollectionResult } from "./openItemsCollectionResult";
import { UploadEventPort } from "./uploadEventPort";
import { UploadErrorFactoryPort, UploadInfrastructurePort, UploadValidationPort } from './uploadInfrastructurePort';
import { UploadUiPort } from './uploadUiPort';
import {
  getNotExistingItems,
  markItemsForOverwrite,
  prepareExistingItemsForRewrite,
  toExistsCheckResult,
} from "./uploadCore";

interface UploaderHandlerDependencies {
  createUploadItemRow: (uploadItem: IUploadItem) => UploadItemRow;
  infrastructure: UploadInfrastructurePort;
  errors: UploadErrorFactoryPort;
  validation: UploadValidationPort;
}

const validateUploadItems = (
  uploadItems: IUploadItem[],
  dependencies: UploaderHandlerDependencies
) => {
  for (let i = 0; i < uploadItems.length; i++) {
    const validationError = validateName(uploadItems[i], dependencies);
    if (validationError) {
      return validationError;
    }
  }
};

const validateName = (uploadItem: IUploadItem, dependencies: UploaderHandlerDependencies) => {
  const validationMessage = dependencies.validation.validateName(uploadItem.GetName());
  if (validationMessage) {
    return dependencies.errors.createValidationError(validationMessage, uploadItem.GetUrl());
  }
};

const openItemsCollectionAsync = (
  uploadItems: IUploadItem[],
  dependencies: UploaderHandlerDependencies
): Promise<OpenItemsCollectionResult[]> => {
  if (uploadItems.length === 0) return Promise.resolve([]);

  return Promise.all(
    uploadItems.map(
      (uploadItem) =>
        new Promise<OpenItemsCollectionResult>((resolve) => {
          dependencies.infrastructure.openItemCallback(
            dependencies.infrastructure.encodeUri(uploadItem.GetUrl()),
            (asyncResult) => resolve({ uploadItem, asyncResult })
          );
        })
    )
  );
};

const getExistsAsync = async (
  uploadItems: IUploadItem[],
  dependencies: UploaderHandlerDependencies
) => {
  const resultCollection = await openItemsCollectionAsync(uploadItems, dependencies);
  return toExistsCheckResult(resultCollection);
};

interface IQueueChangedEvent {
  AddedItems: IUploadItem[];
  RemovedItems: IUploadItem[];
}

interface IUploadItemsCreatedEvent {
  Items: IUploadItem[];
  Upload(items: IUploadItem[]): void;
}

export interface UploaderHandlers {
  onQueueChanged: (e: IQueueChangedEvent) => void;
  onUploadItemsCreated: (e: IUploadItemsCreatedEvent) => void;
}

export function createUploaderHandlers(
  upload: UploadUiPort,
  events: UploadEventPort,
  dependencies: UploaderHandlerDependencies
): UploaderHandlers {
  const handlers: UploaderHandlers = {
    onQueueChanged: (e: IQueueChangedEvent) => {
      // Display each item added to the upload queue in the grid.
      e.AddedItems.forEach(function (uploadItem) {
        const uploadItemRow = dependencies.createUploadItemRow(uploadItem);
        uploadItemRow.addHandlers(
          createUploadItemHandlers(
            uploadItemRow,
            upload,
            events,
            dependencies.infrastructure,
            dependencies.errors
          )
        );
        upload.addUploadItemRow(uploadItemRow);
      });

      e.RemovedItems.forEach(function (uploadItem) {
        upload.removeUploadItemRow(uploadItem);
      });
    },
    onUploadItemsCreated: async (
      e: IUploadItemsCreatedEvent
    ) => {
      /* Validate file extensions, size, name, etc. here. */
      const validationError = validateUploadItems(e.Items, dependencies);
      if (validationError) {
        events.onErrorOccurred(validationError);
        return;
      }

      /* Below we will check if each file exists on the server
            and ask a user if files should be overwritten or skipped. */
      const existsCheckResult = await getExistsAsync(e.Items, dependencies);

      if (existsCheckResult.IsSuccess && (existsCheckResult.Result as IUploadItem[]).length === 0) {
        // No items exist on the server. Add all items to the upload queue.
        e.Upload(e.Items);
        return;
      }

      if (!existsCheckResult.IsSuccess) {
        // Some error occurred during item existence verification.
        // Show error dialog and mark all items as failed.
        events.onErrorOccurred(dependencies.errors.createExistsCheckError(existsCheckResult.Error));

        e.Items.forEach(function (uploadItem) {
          uploadItem.SetFailed(existsCheckResult.Error);
        });

        // Add all items to the upload queue so a user can retry later.
        e.Upload(e.Items);
        return;
      }

      const existingUploadItems = existsCheckResult.Result as IUploadItem[];
      const itemsList = prepareExistingItemsForRewrite(existingUploadItems);

      const onOverwrite = function () {
        markItemsForOverwrite(existingUploadItems);
        e.Upload(e.Items);
      };

      const onSkipExists = function () {
        const notExistingUploadItems = getNotExistingItems(e.Items, existingUploadItems);
        e.Upload(notExistingUploadItems);
      };

      /* One or more items exists on the server. Show Overwrite / Skip / Cancel dialog.*/
      const rewriteData = new RewriteItemsData(onOverwrite, onSkipExists, itemsList);
      upload.setRewriteItemsData(rewriteData);
    },
  };

  return handlers;
}
