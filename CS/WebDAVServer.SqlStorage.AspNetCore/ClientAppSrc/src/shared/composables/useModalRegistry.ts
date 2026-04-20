import { type ComponentType } from 'react';

export interface ModalConfig<T = unknown> {
  component: ComponentType<ModalComponentProps<T> & Record<string, unknown>>;
  props: Record<string, unknown>;
  resolve: (value: T | undefined) => void;
}

export interface ModalComponentProps<T = void> {
  onSubmit: (value?: T) => Promise<void>;
  onClose: () => void;
}

type ModalListener = (config: ModalConfig | null) => void;

let _activeModal: ModalConfig | null = null;
const _listeners = new Set<ModalListener>();

export const isAnyModalOpen = {
  get value() {
    return _activeModal !== null;
  },
};

function notify() {
  _listeners.forEach(l => l(_activeModal));
}

export function subscribeModal(listener: ModalListener): () => void {
  _listeners.add(listener);
  listener(_activeModal);
  return () => {
    _listeners.delete(listener);
  };
}

export function showModalComponent<T = void>(
  component: ComponentType<ModalComponentProps<T> & Record<string, unknown>>,
  props: Record<string, unknown> = {}
): Promise<T | undefined> {
  return new Promise<T | undefined>(resolve => {
    _activeModal = {
      component: component as ComponentType<ModalComponentProps<unknown> & Record<string, unknown>>,
      props,
      resolve: resolve as (value: unknown) => void,
    };
    notify();
  });
}

export function closeModal(): void {
  _activeModal = null;
  notify();
}
