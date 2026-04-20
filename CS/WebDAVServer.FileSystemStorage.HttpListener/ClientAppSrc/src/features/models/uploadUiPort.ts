import type { IUploadItem, UploadItemRow } from './uploadItemRow';
import type { RewriteItemsData } from './rewriteItemsData';

export interface UploadUiPort {
  addUploadItemRow: (uploadItemRow: UploadItemRow) => void;
  removeUploadItemRow: (uploadItem: IUploadItem) => void;
  setRewriteItemsData: (data: RewriteItemsData | null) => void;
}
