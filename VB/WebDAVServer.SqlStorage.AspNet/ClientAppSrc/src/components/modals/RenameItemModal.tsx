import { useState } from 'react';
import { DefaultModal } from './DefaultModal';
import { modalFormStyles } from './modalFormStyles';
import { ValidationError } from '@/shared/types/appErrors';
import { FormatUtils } from '@/shared/utils/formatUtils';
import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { t } from '@/shared/i18n/translate';

interface Props {
  onSubmit: () => Promise<void>;
  onClose: () => void;
  item?: HierarchyItem;
  onSubmitAction?: (newName: string) => void | Promise<void>;
}

const INVALID_CHARS_TEMPLATE = `${t('phrases.validations.notContainFollowingCharacters')}: {0}`;

function validateItemName(name: string): string | null {
  if (!name.trim()) return t('phrases.validations.nameIsRequired');
  return FormatUtils.validateName(name, INVALID_CHARS_TEMPLATE) ?? null;
}

export function RenameItemModal({ onSubmit, onClose, item, onSubmitAction }: Props) {
  const [itemName, setItemName] = useState(item?.name ?? '');
  const [errorMessage, setErrorMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const oldItemName = item?.name ?? '';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (oldItemName === itemName) {
      onClose();
      return;
    }

    const validationError = validateItemName(itemName);
    if (validationError) {
      setErrorMessage(validationError);
      return;
    }

    setErrorMessage('');
    setIsSubmitting(true);
    try {
      if (item && onSubmitAction) {
        await onSubmitAction(itemName.trim());
      }
      await onSubmit();
    } catch (error) {
      if (error instanceof ValidationError) {
        setErrorMessage(error.message);
      } else if (error instanceof Error) {
        setErrorMessage(error.message || t('phrases.errors.renameFailed'));
      } else {
        setErrorMessage(t('phrases.errors.renameFailed'));
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <DefaultModal title={t('phrases.modals.renameItemTitle')} closeModal={onClose}>
      <form onSubmit={handleSubmit}>
        <div className={modalFormStyles.body}>
          <div className="space-y-4">
            <div>
              <input
                autoFocus
                type="text"
                className={modalFormStyles.input}
                placeholder={t('phrases.modals.itemNamePlaceholder')}
                value={itemName}
                disabled={isSubmitting}
                onChange={e => {
                  setItemName(e.target.value);
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
            {isSubmitting ? t('phrases.loading') : t('phrases.ok')}
          </button>
        </div>
      </form>
    </DefaultModal>
  );
}
