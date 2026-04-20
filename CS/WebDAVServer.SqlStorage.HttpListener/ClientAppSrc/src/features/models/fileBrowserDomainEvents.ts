export interface FileBrowserDomainEventPort {
  onFolderRefreshRequested: () => void;
  onItemUpdated: (fullPath: string) => void;
  onError: (error: unknown) => void;
}
