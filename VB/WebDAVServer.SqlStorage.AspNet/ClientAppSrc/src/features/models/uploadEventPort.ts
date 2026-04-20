export interface UploadEventPort {
  onFolderRefreshRequested: () => void;
  onErrorOccurred: (error: unknown) => void;
}
