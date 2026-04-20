import { useRef, useState } from 'react';
import { DefaultModal } from './DefaultModal';
import { modalFormStyles } from './modalFormStyles';
import { t } from '@/shared/i18n/translate';

interface Props {
  onSubmit: () => Promise<void>;
  onClose: () => void;
  message: string;
  onSubmitAction?: () => void | Promise<void>;
}

export function SubmitModal({ onSubmit, onClose, message, onSubmitAction }: Props) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const submitRef = useRef<HTMLButtonElement>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    setSubmitError('');
    try {
      if (onSubmitAction) {
        await onSubmitAction();
      }
      await onSubmit();
    } catch (error) {
      setSubmitError((error as Error)?.message || t('phrases.errors.unknown'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <DefaultModal title={t('phrases.modals.defaultModalTitle')} closeModal={onClose}>
      <form onSubmit={handleSubmit}>
        <div className={modalFormStyles.body}>
          <div className="flex items-center gap-3 p-4 bg-info-subtle border border-info rounded-lg">
            <svg
              className="w-6 h-6 text-info shrink-0"
              fill="none"
              stroke="currentColor"
              viewBox="0 0 24 24"
            >
              <path
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeWidth={2}
                d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
              />
            </svg>
            <p className="text-sm text-secondary leading-relaxed">{message}</p>
          </div>
          {submitError && <p className="mt-2 text-sm text-error">{submitError}</p>}
        </div>
        <div className={modalFormStyles.footer}>
          <button
            type="button"
            className={modalFormStyles.buttonSecondary}
            disabled={isSubmitting}
            onClick={onClose}
          >
            {t('phrases.cancel')}
          </button>
          <button
            ref={submitRef}
            autoFocus
            type="submit"
            className={modalFormStyles.buttonPrimary}
            disabled={isSubmitting}
          >
            {isSubmitting ? t('phrases.loading') : t('phrases.ok')}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
}
