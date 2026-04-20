import { useRef, useState } from 'react';
import { DefaultModal } from './DefaultModal';
import { modalFormStyles } from './modalFormStyles';
import { ValidationError } from '@/shared/types/appErrors';
import { FormatUtils } from '@/shared/utils/formatUtils';
import { t } from '@/shared/i18n/translate';

interface Props {
  onSubmit: () => Promise<void>;
  onClose: () => void;
  onSubmitAction?: (folderName: string) => void | Promise<void>;
}

const INVALID_CHARS_TEMPLATE = `${t('phrases.validations.notContainFollowingCharacters')}: {0}`;

function validateFolderName(name: string): string | null {
  if (!name.trim()) return t('phrases.validations.nameIsRequired');
  return FormatUtils.validateName(name, INVALID_CHARS_TEMPLATE) ?? null;
}

export function CreateFolderModal({ onSubmit, onClose, onSubmitAction }: Props) {
  const [folderName, setFolderName] = useState('');
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErrorMessage('');

    const validationError = validateFolderName(folderName);
    if (validationError) {
      setErrorMessage(validationError);
      return;
    }

    setIsSubmitting(true);
    try {
      if (onSubmitAction) {
        await onSubmitAction(folderName.trim());
      }
      await onSubmit();
    } catch (error) {
      if (error instanceof ValidationError) {
        setErrorMessage(error.message);
      } else if (
        error &&
        typeof error === 'object' &&
        'message' in error &&
        typeof error.message === 'string' &&
        error.message.includes('MethodNotAllowed')
      ) {
        setErrorMessage(t('phrases.validations.folderExists'));
      } else {
        setErrorMessage((error as Error).message || t('phrases.errors.createFolderErrorMessage'));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <DefaultModal title={t('phrases.modals.createFolderTitle')} closeModal={onClose}>
      <form onSubmit={handleSubmit}>
        <div className={modalFormStyles.body}>
          <div className="space-y-4">
            <div>
              <input
                ref={inputRef}
                autoFocus
                type="text"
                className={modalFormStyles.input}
                placeholder={t('phrases.modals.folderNamePlaceholder')}
                value={folderName}
                disabled={isSubmitting}
                onChange={e => {
                  setFolderName(e.target.value);
                  if (errorMessage) setErrorMessage('');
                }}
              />
              {errorMessage && (
                <div className={modalFormStyles.errorBox}>
                  <svg
                    className="w-5 h-5 text-error shrink-0 mt-0.5"
                    fill="none"
                    stroke="currentColor"
                    viewBox="0 0 24 24"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      strokeWidth={2}
                      d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"
                    />
                  </svg>
                  <span className={modalFormStyles.errorText}>{errorMessage}</span>
                </div>
              )}
            </div>
          </div>
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
          <button type="submit" className={modalFormStyles.buttonPrimary} disabled={isSubmitting}>
            {isSubmitting ? (
              <svg className="w-4 h-4 animate-spin" fill="none" viewBox="0 0 24 24">
                <circle
                  className="opacity-25"
                  cx="12"
                  cy="12"
                  r="10"
                  stroke="currentColor"
                  strokeWidth="4"
                />
                <path
                  className="opacity-75"
                  fill="currentColor"
                  d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"
                />
              </svg>
            ) : null}
            {isSubmitting ? t('phrases.creating') : t('phrases.ok')}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
}
