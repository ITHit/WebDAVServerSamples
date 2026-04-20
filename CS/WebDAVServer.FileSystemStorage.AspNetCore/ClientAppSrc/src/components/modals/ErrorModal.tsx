import { useState } from 'react';
import { DefaultModal } from '@/components/modals/DefaultModal';
import { modalFormStyles } from '@/components/modals/modalFormStyles';
import type { ModalComponentProps } from '@/shared/composables/useModalRegistry';
import { t } from '@/shared/i18n/translate';

type ErrorModalPayload = {
  errorMessage: string;
  isHttpError?: () => boolean;
  getUri?: () => string | null;
  getServerMessage?: () => string;
};

function isErrorModalPayload(value: unknown): value is ErrorModalPayload {
  if (!value || typeof value !== 'object') {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return typeof candidate.errorMessage === 'string';
}

interface Props extends ModalComponentProps<void> {
  error: unknown;
}

export function ErrorModal({ error, onClose }: Props) {
  const [isOpenedDetails, setIsOpenedDetails] = useState(false);
  const webDavError = isErrorModalPayload(error) ? error : null;

  const isHttpError = webDavError?.isHttpError?.() ?? false;
  const uri = webDavError?.getUri?.() ?? '';
  const serverMessage = webDavError?.getServerMessage?.() ?? '';

  return (
    <DefaultModal
      title={t('phrases.modals.errorTitle')}
      dialogClassName="modal-lg"
      closeModal={onClose}
    >
      <div className={modalFormStyles.body}>
        <div className="space-y-4">
          <div className="flex items-start gap-4 p-4 bg-error-subtle border border-error rounded-lg">
            <div className="shrink-0">
              <svg
                className="w-10 h-10 text-error"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth="2"
                  d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                />
              </svg>
            </div>
            <div className="flex-1 min-w-0">
              <h4 className="text-sm font-medium text-secondary mb-1">
                {t('phrases.errors.errorMessage')}
              </h4>
              <p className="text-base text-error font-medium wrap-break-word">
                {webDavError?.errorMessage ?? t('phrases.errors.unknown')}
              </p>
            </div>
          </div>

          <div className="space-y-2">
            <button
              type="button"
              className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-secondary bg-surface-secondary hover:bg-surface-hover rounded-lg transition-colors"
              onClick={() => setIsOpenedDetails(previous => !previous)}
            >
              <svg
                className={[
                  'w-4 h-4 transition-transform',
                  isOpenedDetails ? 'rotate-90' : '',
                ].join(' ')}
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth="2"
                  d="M9 5l7 7-7 7"
                />
              </svg>
              {t('phrases.errors.errorDetails')}
            </button>

            {isOpenedDetails ? (
              <div className="p-4 bg-surface-secondary border border-border rounded-lg space-y-4">
                {isHttpError ? (
                  <div className="space-y-1">
                    <p className="text-xs font-semibold text-muted uppercase tracking-wide">
                      {t('phrases.url')}
                    </p>
                    <p className="text-sm text-foreground font-mono bg-surface px-3 py-2 rounded border break-all">
                      {uri}
                    </p>
                  </div>
                ) : null}

                <div className="space-y-1">
                  <p className="text-xs font-semibold text-muted uppercase tracking-wide">
                    {t('phrases.errors.serverMessage')}
                  </p>
                  <div className="text-sm text-foreground bg-surface px-3 py-2 rounded border overflow-auto max-h-48 whitespace-pre-wrap">
                    {serverMessage}
                  </div>
                </div>
              </div>
            ) : null}
          </div>
        </div>
      </div>

      <form
        onSubmit={event => {
          event.preventDefault();
          onClose();
        }}
      >
        <div className={modalFormStyles.footer}>
          <button type="submit" className={modalFormStyles.buttonPrimary}>
            {t('phrases.close')}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
}
