import { AppRouter } from '@/app/routes/AppRouter';
import { ErrorModalListener } from '@/components/modals/ErrorModalListener';
import { ModalManager } from '@/components/modals/ModalManager';
import { ToastContainer } from '@/components/toast/ToastContainer';

function App() {
  return (
    <>
      <AppRouter />
      <ErrorModalListener />
      <ModalManager />
      <ToastContainer />
    </>
  );
}

export default App;
