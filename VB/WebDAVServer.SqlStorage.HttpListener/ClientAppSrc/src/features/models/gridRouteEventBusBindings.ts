import { APP_EVENTS, type AppEventBus } from '@/shared/contracts/appEventBus';

export interface GridRouteEventBusBindingsDeps {
  eventBus: AppEventBus;
  onFolderRefreshRequested: () => void;
  onItemUpdated: (path: string) => void;
  onErrorOccurred: (error: unknown) => void;
}

export function bindGridRouteEventBusHandlers(deps: GridRouteEventBusBindingsDeps): () => void {
  deps.eventBus.on(APP_EVENTS.FOLDER_REFRESH_REQUESTED, deps.onFolderRefreshRequested);
  deps.eventBus.on(APP_EVENTS.ITEM_UPDATED, deps.onItemUpdated);
  deps.eventBus.on(APP_EVENTS.ERROR_OCCURRED, deps.onErrorOccurred);

  return () => {
    deps.eventBus.off(APP_EVENTS.FOLDER_REFRESH_REQUESTED, deps.onFolderRefreshRequested);
    deps.eventBus.off(APP_EVENTS.ITEM_UPDATED, deps.onItemUpdated);
    deps.eventBus.off(APP_EVENTS.ERROR_OCCURRED, deps.onErrorOccurred);
  };
}
