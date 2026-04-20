export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
  id: string;
  message: string;
  type: ToastType;
  duration: number;
}

type ToastListener = (toasts: Toast[]) => void;

let _toasts: Toast[] = [];
let _toastId = 0;
const _listeners = new Set<ToastListener>();

function notify() {
  const snapshot = [..._toasts];
  _listeners.forEach(l => l(snapshot));
}

function show(message: string, type: ToastType = 'info', duration = 5000): string {
  const id = `toast-${++_toastId}`;
  _toasts = [..._toasts, { id, message, type, duration }];
  notify();

  if (duration > 0) {
    setTimeout(() => remove(id), duration);
  }

  return id;
}

function remove(id: string): void {
  _toasts = _toasts.filter(t => t.id !== id);
  notify();
}

function clear(): void {
  _toasts = [];
  notify();
}

export function subscribeToasts(listener: ToastListener): () => void {
  _listeners.add(listener);
  listener([..._toasts]);
  return () => {
    _listeners.delete(listener);
  };
}

export const toast = {
  show,
  remove,
  clear,
  success: (message: string, duration?: number) => show(message, 'success', duration),
  error: (message: string, duration?: number) => show(message, 'error', duration),
  warning: (message: string, duration?: number) => show(message, 'warning', duration),
  info: (message: string, duration?: number) => show(message, 'info', duration),
};
