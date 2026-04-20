import { useEffect, useRef, useState } from 'react';
import { defaultHotkeys, defaultToolbarHotkeys } from '@/shared/config/hotkey-config';
import { t } from '@/shared/i18n/translate';

const navigationHotkeys = defaultHotkeys.filter(hotkey => hotkey.description);
const fileActionHotkeys = defaultToolbarHotkeys.filter(hotkey => hotkey.description);

const KEY_DISPLAY: Record<string, string> = {
  ArrowDown: '↓',
  ArrowUp: '↑',
  ArrowLeft: '←',
  ArrowRight: '→',
  Escape: 'Esc',
  Delete: 'Del',
  Enter: 'Enter',
};

function splitShortcut(shortcut: string): string[] {
  return shortcut.split('+');
}

function displayKey(key: string): string {
  return KEY_DISPLAY[key] ?? key;
}

export function HotkeyInfo() {
  const [isOpen, setIsOpen] = useState(false);
  const panelRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    const handleOutsideClick = (event: MouseEvent) => {
      if (panelRef.current && !panelRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    const handleKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleOutsideClick);
    document.addEventListener('keydown', handleKeyDown);

    return () => {
      document.removeEventListener('mousedown', handleOutsideClick);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, []);

  return (
    <div className="relative" ref={panelRef}>
      <button
        type="button"
        title={t('phrases.hotkeys.panelTitle')}
        aria-expanded={isOpen}
        aria-haspopup="dialog"
        className="flex items-center justify-center w-7 h-7 rounded text-muted hover:text-foreground hover:bg-surface-hover transition-colors cursor-pointer"
        onClick={() => setIsOpen(previous => !previous)}
      >
        <i className="icon icon-sign-i" style={{ width: '1.5rem', height: '1.5rem' }} />
      </button>

      {isOpen ? (
        <div
          className="absolute right-0 top-full mt-1 z-50 w-72 rounded-lg border border-border bg-surface shadow-lg overflow-hidden"
          role="dialog"
          aria-label={t('phrases.hotkeys.panelTitle')}
        >
          <div className="px-3 py-2 border-b border-border">
            <h3 className="text-sm font-semibold text-foreground">
              {t('phrases.hotkeys.panelTitle')}
            </h3>
          </div>

          <div className="overflow-y-auto max-h-80 py-1">
            <div className="px-3 pt-2 pb-1">
              <p className="text-xs text-center font-medium text-muted uppercase tracking-wide mb-1">
                {t('phrases.hotkeys.navigationGroup')}
              </p>
            </div>
            <ul>
              {navigationHotkeys.map(hotkey => (
                <li key={hotkey.id} className="flex items-center justify-between px-3 py-1 gap-4">
                  <span className="flex items-center gap-0.5 shrink-0">
                    {splitShortcut(hotkey.shortcut).map((key, index) => (
                      <span key={`${hotkey.id}-${key}`} className="flex items-center">
                        {index > 0 ? <span className="text-xs text-muted mx-0.5">+</span> : null}
                        <kbd className="inline-flex items-center justify-center min-w-[1.5rem] px-1.5 py-0.5 text-xs font-mono rounded border border-border bg-surface-hover text-foreground">
                          {displayKey(key)}
                        </kbd>
                      </span>
                    ))}
                  </span>
                  <span className="text-sm text-foreground text-right">{hotkey.description}</span>
                </li>
              ))}
            </ul>

            <div className="px-3 pt-3 pb-1">
              <p className="text-xs text-center font-medium text-muted uppercase tracking-wide mb-1">
                {t('phrases.hotkeys.fileActionsGroup')}
              </p>
            </div>
            <ul>
              {fileActionHotkeys.map(hotkey => (
                <li key={hotkey.id} className="flex items-center justify-between px-3 py-1 gap-4">
                  <span className="flex items-center gap-0.5 shrink-0">
                    {splitShortcut(hotkey.shortcut).map((key, index) => (
                      <span key={`${hotkey.id}-${key}`} className="flex items-center">
                        {index > 0 ? <span className="text-xs text-muted mx-0.5">+</span> : null}
                        <kbd className="inline-flex items-center justify-center min-w-[1.5rem] px-1.5 py-0.5 text-xs font-mono rounded border border-border bg-surface-hover text-foreground">
                          {displayKey(key)}
                        </kbd>
                      </span>
                    ))}
                  </span>
                  <span className="text-sm text-foreground text-right">{hotkey.description}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      ) : null}
    </div>
  );
}
