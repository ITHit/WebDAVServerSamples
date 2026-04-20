import { ToastItem } from './ToastItem';
import { toast } from '@/shared/composables/useToast';
import { useToasts } from '@/features/hooks/useToasts';

export function ToastContainer() {
  const toasts = useToasts();

  return (
    <div className="toast-container fixed top-4 right-4 z-50 max-w-md">
      {toasts.map(item => (
        <ToastItem key={item.id} toast={item} onClose={toast.remove} />
      ))}
    </div>
  );
}
