import { useMemo, useState } from 'react';
import { DefaultModal } from '@/components/modals/DefaultModal';
import { modalFormStyles } from '@/components/modals/modalFormStyles';
import { t } from '@/shared/i18n/translate';
import { decode } from '@/shared/utils/urlCodec';

interface Props {
  onSubmit: () => Promise<void>;
  onClose: () => void;
  itemsList: string[];
  onSubmitAction?: () => void | Promise<void>;
  onSkipAction?: () => void | Promise<void>;
}

export function RewriteModal({
  onSubmit,
  onClose,
  itemsList,
  onSubmitAction,
  onSkipAction,
}: Props) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const decodedItemsList = useMemo(() => itemsList.map(item => decode(item)), [itemsList]);

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
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

  const handleSkip = async () => {
    setIsSubmitting(true);
    setSubmitError('');

    try {
      if (onSkipAction) {
        await onSkipAction();
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
          <div className="flex justify-center mb-4">
            <div className="rounded-full bg-warning-subtle p-3">
              <svg
                className="w-10 h-10 text-warning"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24"
              >
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z"
                />
              </svg>
            </div>
          </div>

          <div className="space-y-3 text-center">
            <p className="text-secondary font-medium">
              {t('phrases.validations.followingItemExist')}:
            </p>

            <div className="bg-warning-subtle border border-warning rounded-lg p-3 text-left">
              {decodedItemsList.map((item, index) => (
                <p key={`${item}-${index}`} className="text-sm text-foreground font-mono">
                  {item}
                </p>
              ))}
            </div>

            <p className="text-secondary font-medium">{t('phrases.overwrite')}?</p>
          </div>

          {submitError ? (
            <p className="mt-3 text-sm text-error text-center">{submitError}</p>
          ) : null}
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
            type="button"
            className={modalFormStyles.buttonSecondary}
            disabled={isSubmitting}
            onClick={handleSkip}
          >
            {t('phrases.noToAll')}
          </button>
          <button type="submit" className={modalFormStyles.buttonPrimary} disabled={isSubmitting}>
            {isSubmitting ? t('phrases.loading') : t('phrases.yesToAll')}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
}
