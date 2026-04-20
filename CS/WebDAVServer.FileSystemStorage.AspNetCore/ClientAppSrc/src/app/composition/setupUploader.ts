import { Uploader } from '@/features/models/uploader';
import { createUploaderHandlers } from '@/features/models/uploaderHandlers';
import { createUploadEventAdapter } from '@/features/adapters/react/createUploadEventAdapter';
import type { AppEventBus } from '@/shared/contracts/appEventBus';
import type { UploadState } from '@/features/hooks/useUpload';
import { getAppServices } from '@/app/appServices';
import type { IUploadItem, UploadItemRow } from '@/features/models/uploadItemRow';

type AppServices = ReturnType<typeof getAppServices>;

interface SetupUploaderDeps {
  eventBus: AppEventBus;
  runtime: {
    getServerOrigin: () => string;
    getServerUrl: (pathName: string) => string;
    decode: (path: string) => string;
    encodeUri: (text: string) => string;
    onPathChange: (handler: (newPath: string, oldPath: string) => void) => void;
  };
  createUploadItemRow: (uploadItem: IUploadItem) => UploadItemRow;
  adaptUploadItemRow?: (uploadItemRow: UploadItemRow) => UploadItemRow;
  validation: {
    validateName: (name: string) => string | undefined;
  };
  errors: {
    createValidationError: (validationMessage: string, itemUrl: string) => unknown;
    createExistsCheckError: (originalError: unknown) => unknown;
  };
}

export function setupUploader(upload: UploadState, appServices: AppServices, deps: SetupUploaderDeps) {
  const currentUrl = deps.runtime.getServerUrl(window.location.pathname);
  const adaptUploadItemRow = deps.adaptUploadItemRow ?? (uploadItemRow => uploadItemRow);

  const uploader = new Uploader(
    currentUrl,
    createUploaderHandlers(upload, createUploadEventAdapter(deps.eventBus), {
      createUploadItemRow: uploadItem => adaptUploadItemRow(deps.createUploadItemRow(uploadItem)),
      infrastructure: {
        encodeUri: deps.runtime.encodeUri,
        openItemCallback: appServices.openItemCallback,
      },
      errors: deps.errors,
      validation: deps.validation,
    }),
    appServices.createUploaderCore(),
    deps.runtime.decode
  );

  upload.setUploader(uploader);

  deps.runtime.onPathChange((newPath, oldPath) => {
    if (newPath !== oldPath && upload.uploader !== null) {
      upload.uploader.setUploadUrl(deps.runtime.getServerUrl(newPath));
    }
  });

  return uploader;
}
