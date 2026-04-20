import { type ReactNode, useEffect, useRef } from 'react';
import { t } from '@/shared/i18n/translate';

interface Props {
  title: string;
  dialogClassName?: string;
  closeModal: () => void;
  children: ReactNode;
}

const SIZE_CLASSES: Record<string, string> = {
  'modal-sm': 'max-w-96',
  'modal-md': 'max-w-lg',
  'modal-lg': 'max-w-3xl',
  'modal-xl': 'max-w-4xl',
};

export function DefaultModal({ title, dialogClassName = 'modal-md', closeModal, children }: Props) {
  const dialogRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        closeModal();
      }
    };
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [closeModal]);

  const sizeClass = SIZE_CLASSES[dialogClassName] ?? SIZE_CLASSES['modal-md'];

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto" role="dialog" aria-modal="true">
      {/* Backdrop */}
      <div className="fixed inset-0 bg-black/50 backdrop-blur-sm" onClick={closeModal} />

      {/* Modal container */}
      <div className="flex min-h-full items-center justify-center p-4">
        <div
          ref={dialogRef}
          className={`relative w-full ${sizeClass} bg-surface rounded-xl shadow-2xl`}
          onClick={e => e.stopPropagation()}
        >
          {/* Header */}
          <div className="flex items-center justify-between px-6 py-4 border-b border-border">
            <h3 className="text-lg font-semibold text-foreground">{title}</h3>
            <button
              type="button"
              className="icon-btn text-muted hover:text-secondary rounded-lg focus:outline-none focus:ring-2 focus:ring-primary"
              aria-label={t('phrases.close')}
              onClick={closeModal}
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path
                  strokeLinecap="round"
                  strokeLinejoin="round"
                  strokeWidth={2}
                  d="M6 18L18 6M6 6l12 12"
                />
              </svg>
            </button>
          </div>

          {/* Body */}
          {children}
        </div>
      </div>
    </div>
  );
}
