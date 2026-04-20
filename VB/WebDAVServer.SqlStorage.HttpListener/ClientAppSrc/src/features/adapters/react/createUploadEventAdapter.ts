import { APP_EVENTS, type AppEventBus } from '@/shared/contracts/appEventBus';
import type { UploadEventPort } from '@/features/models/uploadEventPort';

export function createUploadEventAdapter(eventBus: AppEventBus): UploadEventPort {
  return {
    onFolderRefreshRequested: () => {
      eventBus.emit(APP_EVENTS.FOLDER_REFRESH_REQUESTED);
    },
    onErrorOccurred: (error: unknown) => {
      eventBus.emit(APP_EVENTS.ERROR_OCCURRED, error);
    },
  };
}
