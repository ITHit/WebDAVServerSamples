import '@/infrastructure/webdav/initializeLicense';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import './styles/index.css';
import App from './App';
import { STORAGE_KEY } from '@/shared/composables/useThemeMode';

// Initialize theme before mounting the app
const initTheme = () => {
  const savedTheme =
    (localStorage.getItem(STORAGE_KEY) as 'light' | 'dark' | 'system' | null) || 'system';

  const getSystemPreference = () => {
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  };

  const isDark = savedTheme === 'dark' || (savedTheme === 'system' && getSystemPreference());

  if (isDark) {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
};

initTheme();

createRoot(document.getElementById('app')!).render(
  <StrictMode>
    <BrowserRouter>
      <App />
    </BrowserRouter>
  </StrictMode>
);
