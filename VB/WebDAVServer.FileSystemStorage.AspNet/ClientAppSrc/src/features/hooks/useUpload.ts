import { useCallback, useState } from 'react';
import type { RewriteItemsData } from '@/features/models/rewriteItemsData';
import type { IUploadItem, UploadItemRow } from '@/features/models/uploadItemRow';
import type { Uploader } from '@/features/models/uploader';

/**
 * React equivalent of Vue's useUpload composable.
 */
export function useUpload() {
  const [uploadItemRows, setUploadItemRows] = useState<UploadItemRow[]>([]);
  const [rewriteItemsData, setRewriteItemsDataState] = useState<RewriteItemsData | null>(null);
  const [isDragging, setIsDraggingState] = useState(false);
  const [uploader, setUploaderState] = useState<Uploader | null>(null);

  const addUploadItemRow = useCallback((uploadItemRow: UploadItemRow) => {
    setUploadItemRows(prev => [...prev, uploadItemRow]);
  }, []);

  const removeUploadItemRow = useCallback((uploadItem: IUploadItem) => {
    setUploadItemRows(prev =>
      prev.filter(item => {
        if (item.uploadItem === uploadItem) {
          item.destroy();
        }
        return item.uploadItem !== uploadItem;
      })
    );
  }, []);

  const setRewriteItemsData = useCallback((data: RewriteItemsData | null) => {
    setRewriteItemsDataState(data);
  }, []);

  const setIsDragging = useCallback((dragging: boolean) => {
    setIsDraggingState(dragging);
  }, []);

  const setUploader = useCallback((uploaderInstance: Uploader) => {
    setUploaderState(uploaderInstance);
  }, []);

  const clearUploads = useCallback(() => {
    setUploadItemRows(prev => {
      prev.forEach(item => item.destroy());
      return [];
    });
    setRewriteItemsDataState(null);
  }, []);

  return {
    uploadItemRows,
    rewriteItemsData,
    isDragging,
    uploader,
    addUploadItemRow,
    removeUploadItemRow,
    setRewriteItemsData,
    setIsDragging,
    setUploader,
    clearUploads,
  };
}

export type UploadState = ReturnType<typeof useUpload>;
