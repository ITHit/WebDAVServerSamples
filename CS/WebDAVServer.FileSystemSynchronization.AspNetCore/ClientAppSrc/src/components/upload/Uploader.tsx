import { useEffect, useRef, useState } from 'react';
import { RewriteModal } from '@/components/modals/RewriteModal';
import { UploadingItem } from '@/components/upload/UploadingItem';
import { UploadState } from '@/domain/value-objects/UploadState';
import type { UploadState as UploadUiState } from '@/features/hooks/useUpload';
import { showModalComponent } from '@/shared/composables/useModalRegistry';
import { t } from '@/shared/i18n/translate';

interface Props {
  upload: UploadUiState;
}

export function Uploader({ upload }: Props) {
  const [showDetails, setShowDetails] = useState(false);
  const [isPaused, setIsPaused] = useState(false);
  const [, setTicker] = useState(0);
  const openRewriteRef = useRef(false);

  const uploadItemRows = upload.uploadItemRows;

  const isUploading = uploadItemRows.some(
    el => el.currentState !== UploadState.Completed && el.currentState !== UploadState.Canceled
  );

  const calculateUploadedPercent = () => {
    let uploaded = 0;
    if (uploadItemRows.length) {
      uploadItemRows.forEach(el => {
        uploaded += el.uploadItem.GetProgress().Completed;
      });
      uploaded /= uploadItemRows.length;
    }
    return Number.isNaN(uploaded) ? 0 : Math.min(100, Math.round(uploaded));
  };

  useEffect(() => {
    const id = setInterval(() => {
      setTicker(value => value + 1);
    }, 500);

    return () => {
      clearInterval(id);
    };
  }, [uploadItemRows]);

  const uploadedPercent = calculateUploadedPercent();

  useEffect(() => {
    if (!upload.rewriteItemsData || openRewriteRef.current) {
      return;
    }

    openRewriteRef.current = true;
    const rewriteData = upload.rewriteItemsData;

    void showModalComponent<void>(RewriteModal as never, {
      itemsList: rewriteData.itemsList,
      onSubmitAction: async () => {
        rewriteData.onOverwrite();
      },
      onSkipAction: async () => {
        rewriteData.onSkipExists();
      },
    }).finally(() => {
      openRewriteRef.current = false;
      upload.setRewriteItemsData(null);
    });
  }, [upload, upload.rewriteItemsData]);

  const toggleDetails = () => {
    setShowDetails(prev => !prev);
  };

  const pauseAll = () => {
    uploadItemRows.forEach(el => el.pauseClickHandler());
    setIsPaused(true);
  };

  const playAll = () => {
    uploadItemRows.forEach(el => {
      if (el.currentState === UploadState.Paused) {
        el.playClickHandler();
      }
    });
    setIsPaused(false);
  };

  const cancelAll = () => {
    uploadItemRows.forEach(el => el.cancelClickHandler());
  };

  const onActionItem = () => {
    const countPaused = uploadItemRows.filter(el => el.currentState === UploadState.Paused).length;
    const countCompleted = uploadItemRows.filter(
      el => el.currentState === UploadState.Completed || el.currentState === UploadState.Canceled
    ).length;

    if (countPaused === 0) {
      setIsPaused(false);
    } else if (countPaused === uploadItemRows.length - countCompleted) {
      setIsPaused(true);
    }
  };

  if (uploadItemRows.length === 0 || !isUploading) {
    return null;
  }

  return (
    <>
      {!showDetails ? (
        <div className="fixed bottom-4 right-4 z-50 w-72">
          <div className="mt-1 bg-white dark:bg-gray-800 border border-border rounded-lg shadow-lg p-3 transition-opacity duration-300">
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm text-gray-700 dark:text-gray-300">
                {t('phrases.uploader.uploaded')}{' '}
                <span className="font-semibold">{uploadedPercent}%</span>
              </span>
              {!isPaused ? (
                <button
                  className="p-1 rounded hover:bg-surface-hover"
                  title={t('phrases.uploader.pauseUpload')}
                  onClick={pauseAll}
                >
                  <i className="icon icon-pause align-middle" />
                </button>
              ) : null}
              {isPaused ? (
                <button
                  className="p-1 rounded hover:bg-surface-hover"
                  title={t('phrases.uploader.resumeUpload')}
                  onClick={playAll}
                >
                  <i className="icon icon-play align-middle" />
                </button>
              ) : null}
            </div>
            <div className="w-full mb-4">
              <div className="progress-track">
                <div
                  className="progress-fill"
                  role="progressbar"
                  style={{ width: `${uploadedPercent}%` }}
                  aria-valuenow={uploadedPercent}
                  aria-valuemin={0}
                  aria-valuemax={100}
                />
              </div>
            </div>
            <div className="flex gap-2 pt-3 border-t border-border">
              <button
                className="flex-1 px-4 py-2 border border-primary text-primary rounded hover:bg-interactive-subtle text-sm"
                title={t('phrases.uploader.cancelAllUpload')}
                onClick={cancelAll}
              >
                {t('phrases.cancel')}
              </button>
              <button
                className="w-full px-3 py-1.5 bg-primary text-white text-sm rounded hover:bg-primary-hover"
                onClick={toggleDetails}
              >
                {t('phrases.details')}
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {showDetails ? (
        <div className="fixed bottom-4 right-4 z-50 w-100">
          <div className="bg-white dark:bg-gray-800 border border-border rounded-lg shadow-lg w-100">
            <div className="flex items-center justify-between p-3 border-b border-border">
              <span className="font-semibold text-sm text-foreground">
                {t('phrases.uploader.filesUpload')}
              </span>
              <button
                className="p-1 rounded hover:bg-surface-hover"
                title={t('phrases.close')}
                onClick={toggleDetails}
              >
                <i className="icon icon-close" style={{ width: '0.75rem', height: '0.75rem' }} />
              </button>
            </div>
            <div className="max-h-60 overflow-y-auto">
              {uploadItemRows.map((item, index) => (
                <UploadingItem
                  key={`uploading-item-${index}`}
                  uploadItemRow={item}
                  onActionItem={onActionItem}
                />
              ))}
            </div>
            <div className="flex gap-2 p-3 border-t border-border">
              {!isPaused ? (
                <button
                  className="flex-1 px-4 py-2 border border-primary text-primary rounded hover:bg-interactive-subtle text-sm"
                  title={t('phrases.uploader.pauseUpload')}
                  onClick={pauseAll}
                >
                  {t('phrases.uploader.pauseUpload')}
                </button>
              ) : null}
              {isPaused ? (
                <button
                  className="flex-1 px-4 py-2 border border-primary text-primary rounded hover:bg-interactive-subtle text-sm"
                  title={t('phrases.uploader.resumeUpload')}
                  onClick={playAll}
                >
                  {t('phrases.uploader.resumeUpload')}
                </button>
              ) : null}
              <button
                className="flex-1 px-4 py-2 bg-primary text-white rounded hover:bg-primary-hover text-sm"
                title={t('phrases.uploader.cancelAllUpload')}
                onClick={cancelAll}
              >
                {t('phrases.uploader.cancelAllUpload')}
              </button>
            </div>
          </div>
        </div>
      ) : null}
    </>
  );
}
