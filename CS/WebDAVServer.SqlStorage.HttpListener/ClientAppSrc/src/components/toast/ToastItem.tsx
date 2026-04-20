import type { Toast } from '@/shared/composables/useToast';
import { t } from '@/shared/i18n/translate';

interface Props {
  toast: Toast;
  onClose: (id: string) => void;
}

export function ToastItem({ toast, onClose }: Props) {
  const iconClass =
    toast.type === 'success'
      ? 'icon-check'
      : toast.type === 'error'
        ? 'icon-error'
        : toast.type === 'warning'
          ? 'icon-warning'
          : 'icon-info';

  const bgClass =
    toast.type === 'success'
      ? 'bg-success'
      : toast.type === 'error'
        ? 'bg-error'
        : toast.type === 'warning'
          ? 'bg-warning'
          : 'bg-info';

  return (
    <div
      className={[
        'toast-item',
        'flex items-center gap-3 px-4 py-3 rounded-lg shadow-lg mb-3',
        'animate-slide-in-right',
        bgClass,
      ].join(' ')}
      role="alert"
    >
      <i className={`text-xl ${iconClass}`} />
      <p className="flex-1 text-sm font-medium">{toast.message}</p>
      <button
        className="hover:opacity-80 transition-opacity"
        aria-label={t('phrases.toast.closeNotification')}
        onClick={() => onClose(toast.id)}
      >
        <i className="icon-close text-lg" />
      </button>
    </div>
  );
}
