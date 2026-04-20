export interface GridRouteSignalSubscriptions {
  subscribePathAndSearchChanged: (
    handler: (newPath: string, oldPath: string | undefined) => void | Promise<void>
  ) => () => void;
  subscribeSortChanged: (handler: () => void) => () => void;
  subscribeFolderPathChanged: (
    handler: (folderPath: string | undefined) => void | Promise<void>
  ) => () => void;
}

export interface GridRouteSignalHandlers {
  onPathAndSearchChanged: (newPath: string, oldPath: string | undefined) => void | Promise<void>;
  onSortChanged: () => void;
  onFolderPathChanged: (folderPath: string | undefined) => void | Promise<void>;
}

export function bindGridRouteSignals(
  subscriptions: GridRouteSignalSubscriptions,
  handlers: GridRouteSignalHandlers
): () => void {
  const unbindPathAndSearch = subscriptions.subscribePathAndSearchChanged(handlers.onPathAndSearchChanged);
  const unbindSort = subscriptions.subscribeSortChanged(handlers.onSortChanged);
  const unbindFolderPath = subscriptions.subscribeFolderPathChanged(handlers.onFolderPathChanged);

  return () => {
    unbindPathAndSearch();
    unbindSort();
    unbindFolderPath();
  };
}
