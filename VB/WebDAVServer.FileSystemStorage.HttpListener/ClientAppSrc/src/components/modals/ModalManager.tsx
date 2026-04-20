import { useEffect, useState } from 'react';
import { createPortal } from 'react-dom';
import {
  type ModalConfig,
  subscribeModal,
  closeModal,
} from '@/shared/composables/useModalRegistry';

export function ModalManager() {
  const [activeModal, setActiveModal] = useState<ModalConfig | null>(null);

  useEffect(() => {
    return subscribeModal(setActiveModal);
  }, []);

  if (!activeModal) return null;

  const { component: ModalComponent, props, resolve } = activeModal;

  const handleSubmit = async (value: unknown) => {
    resolve(value);
    closeModal();
  };

  const handleClose = () => {
    resolve(undefined);
    closeModal();
  };

  return createPortal(
    <ModalComponent {...props} onSubmit={handleSubmit} onClose={handleClose} />,
    document.body
  );
}
