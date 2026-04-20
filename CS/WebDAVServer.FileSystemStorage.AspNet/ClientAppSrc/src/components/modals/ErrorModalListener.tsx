import { useEffect } from 'react';
import { appEventBus } from '@/app/events/appEventBus';
import { ErrorModal } from '@/components/modals/ErrorModal';
import { showModalComponent } from '@/shared/composables/useModalRegistry';
import { APP_EVENTS } from '@/shared/contracts/appEventBus';

export function ErrorModalListener() {
  useEffect(() => {
    const handleError = (error: unknown) => {
      void showModalComponent(ErrorModal as never, { error });
    };

    appEventBus.on(APP_EVENTS.ERROR_OCCURRED, handleError);

    return () => {
      appEventBus.off(APP_EVENTS.ERROR_OCCURRED, handleError);
    };
  }, []);

  return null;
}
