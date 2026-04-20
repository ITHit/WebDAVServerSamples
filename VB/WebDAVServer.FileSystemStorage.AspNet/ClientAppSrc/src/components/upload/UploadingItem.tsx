import { useEffect } from 'react';
import { UploadState } from '@/domain/value-objects/UploadState';
import type { UploadItemRow } from '@/features/models/uploadItemRow';
import { FormatUtils } from '@/shared/utils/formatUtils';
import { useUploadItemProgress } from '@/shared/composables/useUploadItemProgress';
import { decode } from '@/shared/utils/urlCodec';
import { UploadingFileIcon } from '@/components/upload/UploadingFileIcon';
import { t } from '@/shared/i18n/translate';

interface Props {
  uploadItemRow: UploadItemRow;
  onActionItem: () => void;
}

export function UploadingItem({ uploadItemRow, onActionItem }: Props) {
  const fileName = uploadItemRow.uploadItem.GetName();
  const fileExtension = FormatUtils.getFileExtension(fileName);
  const fileSize = FormatUtils.formatFileSize(uploadItemRow.uploadItem.GetProgress().TotalBytes);

  const { progress, speed } = useUploadItemProgress(uploadItemRow);

  const currentState = uploadItemRow.currentState;
  const isPaused = currentState === UploadState.Paused;

  useEffect(() => {
    onActionItem();
  }, [currentState, onActionItem]);

  const pauseUploading = () => {
    uploadItemRow.pauseClickHandler(onActionItem);
  };

  const playUploading = () => {
    uploadItemRow.playClickHandler(onActionItem);
  };

  const cancelUploading = () => {
    uploadItemRow.cancelClickHandler();
  };

  if (currentState === UploadState.Completed || currentState === UploadState.Canceled) {
    return null;
  }

  return (
    <div className="flex flex-wrap items-center uploading-item px-3 py-1">
      <div className="w-auto px-0 mr-2">
        <button className="p-1 rounded hover:bg-surface-hover" onClick={cancelUploading}>
          <i className="icon icon-close" style={{ width: '0.75rem', height: '0.75rem' }} />
        </button>
      </div>

      <div className="w-auto px-0">
        <UploadingFileIcon fileExtension={fileExtension} />
      </div>

      <div className="flex-1 mx-3">
        <div className="flex items-center justify-between flex-wrap mb-2">
          <div className="w-40 item-name truncate" title={decode(fileName)}>
            {decode(fileName)}
          </div>
          <div className="w-auto item-size">{fileSize}</div>
          {uploadItemRow.retryMessage ? (
            <div className="w-auto text-error">{uploadItemRow.retryMessage}</div>
          ) : null}
        </div>

        <div className="w-full">
          <div className="progress-track">
            <div
              className="progress-fill"
              role="progressbar"
              style={{ width: `${progress}%` }}
              aria-valuenow={progress}
              aria-valuemin={0}
              aria-valuemax={100}
            />
          </div>
        </div>

        <div className="flex justify-between mt-1">
          <div className="w-auto item-progress">
            {progress}% {t('phrases.done')}
          </div>
          <div className="w-auto item-speed">
            {FormatUtils.formatFileSize(speed)}/{t('phrases.secondShortened')}
          </div>
        </div>
      </div>

      <div className="w-auto px-0">
        {!isPaused ? (
          <button className="p-1" onClick={pauseUploading}>
            <i className="icon icon-pause align-middle" />
          </button>
        ) : null}
        {isPaused ? (
          <button className="p-1" onClick={playUploading}>
            <i className="icon icon-play align-middle" />
          </button>
        ) : null}
      </div>
    </div>
  );
}
