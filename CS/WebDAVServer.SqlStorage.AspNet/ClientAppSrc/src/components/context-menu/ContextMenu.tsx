import { useEffect, useLayoutEffect, useMemo, useRef } from 'react';
import { createPortal } from 'react-dom';
import type { ResolvedContextMenuItem } from '@/shared/config/config-types';
import { t } from '@/shared/i18n/translate';

interface Props {
  items: ResolvedContextMenuItem[];
  x: number;
  y: number;
  isVisible: boolean;
  onClose: () => void;
  onSelect?: (item: ResolvedContextMenuItem) => void;
}

export function ContextMenu({ items, x, y, isVisible, onClose, onSelect }: Props) {
  const menuRef = useRef<HTMLDivElement | null>(null);

  const enabledIndexes = useMemo(
    () =>
      items
        .map((item, index) => ({ item, index }))
        .filter(({ item }) => item.type !== 'separator' && !item.disabled)
        .map(({ index }) => index),
    [items]
  );

  useEffect(() => {
    if (!isVisible) {
      return;
    }

    const handleClickOutside = (event: MouseEvent) => {
      if (!menuRef.current?.contains(event.target as Node)) {
        onClose();
      }
    };

    document.addEventListener('click', handleClickOutside);
    document.addEventListener('contextmenu', handleClickOutside);

    return () => {
      document.removeEventListener('click', handleClickOutside);
      document.removeEventListener('contextmenu', handleClickOutside);
    };
  }, [isVisible, onClose]);

  useEffect(() => {
    if (!isVisible) {
      return;
    }

    const firstEnabledIndex = enabledIndexes[0] ?? 0;

    requestAnimationFrame(() => {
      menuRef.current?.focus();
      const firstButton = menuRef.current?.querySelector<HTMLButtonElement>(
        `button[data-menu-index="${firstEnabledIndex}"]`
      );
      firstButton?.focus();
    });
  }, [enabledIndexes, isVisible]);

  useLayoutEffect(() => {
    if (!isVisible) {
      return;
    }

    const padding = 8;
    let nextX = x;
    let nextY = y;

    const menu = menuRef.current;
    if (!menu) {
      return;
    }

    const rect = menu.getBoundingClientRect();

    if (nextX + rect.width > window.innerWidth - padding) {
      nextX = window.innerWidth - rect.width - padding;
    }

    if (nextY + rect.height > window.innerHeight - padding) {
      nextY = window.innerHeight - rect.height - padding;
    }

    menu.style.left = `${Math.max(padding, nextX)}px`;
    menu.style.top = `${Math.max(padding, nextY)}px`;
  }, [isVisible, items, x, y]);

  if (!isVisible) {
    return null;
  }

  const padding = 8;
  const maxWidth = window.innerWidth - padding * 2;
  const maxHeight = window.innerHeight - padding * 2;

  const selectItem = (item: ResolvedContextMenuItem) => {
    if (item.disabled || item.type === 'separator') {
      return;
    }

    onSelect?.(item);
    item.action?.();
    onClose();
  };

  return createPortal(
    <div
      ref={menuRef}
      role="menu"
      aria-label={t('phrases.contextMenu.ariaLabel')}
      tabIndex={-1}
      className="fixed bg-surface rounded-lg shadow-2xl border border-border py-1 min-w-50 outline-none overflow-auto"
      style={{
        top: `${Math.max(padding, y)}px`,
        left: `${Math.max(padding, x)}px`,
        maxWidth: `${maxWidth}px`,
        maxHeight: `${maxHeight}px`,
        zIndex: 9999,
      }}
      onBlur={event => {
        if (!menuRef.current?.contains(event.relatedTarget as Node)) {
          onClose();
        }
      }}
      onKeyDown={event => {
        if (event.key === 'Escape') {
          onClose();
          return;
        }

        if (enabledIndexes.length === 0) {
          return;
        }

        const focusedElement = document.activeElement as HTMLElement | null;
        const focusedRawIndex = focusedElement?.dataset.menuIndex;
        const focusedButtonIndex = focusedRawIndex ? Number.parseInt(focusedRawIndex, 10) : -1;
        const currentEnabledPos = Math.max(0, enabledIndexes.indexOf(focusedButtonIndex));

        if (event.key === 'ArrowDown') {
          event.preventDefault();
          const nextPos = (currentEnabledPos + 1) % enabledIndexes.length;
          const nextIndex = enabledIndexes[nextPos];
          menuRef.current
            ?.querySelector<HTMLButtonElement>(`button[data-menu-index="${nextIndex}"]`)
            ?.focus();
        }

        if (event.key === 'ArrowUp') {
          event.preventDefault();
          const prevPos =
            currentEnabledPos <= 0 ? enabledIndexes.length - 1 : currentEnabledPos - 1;
          const prevIndex = enabledIndexes[prevPos];
          menuRef.current
            ?.querySelector<HTMLButtonElement>(`button[data-menu-index="${prevIndex}"]`)
            ?.focus();
        }
      }}
    >
      <ul role="presentation" className="list-none m-0 p-0">
        {items.map((item, index) => (
          <li key={item.id} role="presentation">
            {item.type === 'separator' ? (
              <hr role="separator" className="my-1 border-0 border-t border-border" />
            ) : (
              <button
                data-menu-index={index}
                role="menuitem"
                type="button"
                disabled={item.disabled}
                aria-label={item.label}
                className={[
                  'w-full flex items-center gap-3 px-3 py-2 text-sm border-none bg-transparent text-left transition-colors cursor-pointer outline-none',
                  item.disabled
                    ? 'opacity-50 cursor-not-allowed text-foreground'
                    : item.danger
                      ? 'text-error hover:bg-surface-hover focus:bg-surface-hover active:bg-surface-hover'
                      : 'text-foreground hover:bg-surface-hover focus:bg-surface-hover active:bg-surface-hover',
                ].join(' ')}
                onClick={() => selectItem(item)}
                onKeyDown={event => {
                  if (event.key === 'Enter' || event.key === ' ') {
                    event.preventDefault();
                    event.stopPropagation();
                    selectItem(item);
                  }
                }}
              >
                {item.icon ? <span className={['icon', item.icon, 'shrink-0'].join(' ')} /> : null}
                <span className="flex-1">{item.label}</span>
                {item.shortcutInfo ? (
                  <kbd className="text-xs text-muted font-mono bg-surface-secondary px-1.5 py-0.5 rounded">
                    {item.shortcutInfo}
                  </kbd>
                ) : null}
              </button>
            )}
          </li>
        ))}
      </ul>
    </div>,
    document.body
  );
}
