import { useThemeMode, type Theme } from '@/shared/composables/useThemeMode';
import { t } from '@/shared/i18n/translate';

export function ThemeToggle() {
  const { theme, isDark, updateTheme, toggleTheme } = useThemeMode();

  return (
    <div className="flex items-center gap-2">
      <button
        className="theme-toggle-btn block sm:hidden"
        title={isDark ? t('phrases.theme.switchToLightMode') : t('phrases.theme.switchToDarkMode')}
        type="button"
        onClick={toggleTheme}
      >
        {!isDark ? (
          <svg className="theme-icon w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2"
              d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z"
            />
          </svg>
        ) : (
          <svg className="theme-icon w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2"
              d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z"
            />
          </svg>
        )}
      </button>

      <select
        value={theme}
        className="theme-select hidden sm:block rounded-md border border-input-border bg-input-bg px-3 py-1.5 text-sm text-input-text scheme-light dark:scheme-dark focus:outline-none"
        aria-label={t('phrases.theme.selectTheme')}
        onChange={e => updateTheme(e.target.value as Theme)}
      >
        <option value="light" className="bg-input text-foreground">
          {t('phrases.theme.light')}
        </option>
        <option value="dark" className="bg-input text-foreground">
          {t('phrases.theme.dark')}
        </option>
        <option value="system" className="bg-input text-foreground">
          {t('phrases.theme.system')}
        </option>
      </select>
    </div>
  );
}
