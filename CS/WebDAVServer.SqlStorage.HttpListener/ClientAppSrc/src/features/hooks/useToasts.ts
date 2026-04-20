import { useEffect, useState } from 'react';
import { type Toast, subscribeToasts } from '@/shared/composables/useToast';

export function useToasts(): Toast[] {
  const [toasts, setToasts] = useState<Toast[]>([]);

  useEffect(() => {
    return subscribeToasts(setToasts);
  }, []);

  return toasts;
}
