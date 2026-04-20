import { useCallback, useEffect, useState } from 'react';

export type Theme = 'light' | 'dark' | 'system';

export const STORAGE_KEY = 'theme-preference';

function getSystemPreference(): boolean {
  if (typeof window.matchMedia !== 'function') {
    return false;
  }
  return window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function getInitialTheme(): Theme {
  return (localStorage.getItem(STORAGE_KEY) as Theme | null) ?? 'system';
}

function getInitialIsDark(theme: Theme): boolean {
  return theme === 'dark' || (theme === 'system' && getSystemPreference());
}

function applyTheme(dark: boolean): void {
  document.documentElement.classList.toggle('dark', dark);
}

export function useThemeMode() {
  const [theme, setTheme] = useState<Theme>(getInitialTheme);
  const [isDark, setIsDark] = useState(() => {
    const t = getInitialTheme();
    return getInitialIsDark(t);
  });

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') {
      return;
    }

    const mediaQuery = window.matchMedia('(prefers-color-scheme: dark)');
    const handleChange = (e: MediaQueryListEvent) => {
      if (theme === 'system') {
        setIsDark(e.matches);
        applyTheme(e.matches);
      }
    };

    mediaQuery.addEventListener('change', handleChange);
    return () => mediaQuery.removeEventListener('change', handleChange);
  }, [theme]);

  const updateTheme = useCallback((newTheme: Theme) => {
    setTheme(newTheme);
    localStorage.setItem(STORAGE_KEY, newTheme);
    const dark = newTheme === 'system' ? getSystemPreference() : newTheme === 'dark';
    setIsDark(dark);
    applyTheme(dark);
  }, []);

  const toggleTheme = useCallback(() => {
    updateTheme(isDark ? 'light' : 'dark');
  }, [isDark, updateTheme]);

  return { theme, isDark, updateTheme, toggleTheme };
}
