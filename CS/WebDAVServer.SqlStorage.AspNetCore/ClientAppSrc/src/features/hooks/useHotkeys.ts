import { useEffect, useEffectEvent, useMemo } from 'react';
import { defaultHotkeys, defaultToolbarHotkeys } from '@/shared/config/hotkey-config';
import type { FileBrowserContext, HotkeyConfig } from '@/shared/config/config-types';
import type { FileBrowserContract } from '@/shared/contracts/fileBrowserContract';

interface ParsedShortcut {
  key: string;
  ctrl: boolean;
  shift: boolean;
  alt: boolean;
  meta: boolean;
}

const KEY_ALIASES: Record<string, string> = {
  del: 'Delete',
  delete: 'Delete',
  esc: 'Escape',
  escape: 'Escape',
  enter: 'Enter',
  space: ' ',
  tab: 'Tab',
  up: 'ArrowUp',
  down: 'ArrowDown',
  left: 'ArrowLeft',
  right: 'ArrowRight',
  home: 'Home',
  end: 'End',
  pageup: 'PageUp',
  pagedown: 'PageDown',
  ins: 'Insert',
  insert: 'Insert',
};

function parseShortcut(shortcut: string): ParsedShortcut {
  const parts = shortcut.split('+');
  const modifiers = parts.slice(0, -1).map(part => part.toLowerCase());
  const rawKey = parts[parts.length - 1];
  const lowerKey = rawKey.toLowerCase();
  const key = KEY_ALIASES[lowerKey] ?? (rawKey.length === 1 ? lowerKey : rawKey);

  return {
    key,
    ctrl: modifiers.includes('ctrl'),
    shift: modifiers.includes('shift'),
    alt: modifiers.includes('alt'),
    meta: modifiers.includes('meta') || modifiers.includes('cmd'),
  };
}

function matchesEvent(parsed: ParsedShortcut, event: KeyboardEvent): boolean {
  const eventKey = event.key.length === 1 ? event.key.toLowerCase() : event.key;
  return (
    eventKey === parsed.key &&
    event.ctrlKey === parsed.ctrl &&
    event.shiftKey === parsed.shift &&
    event.altKey === parsed.alt &&
    event.metaKey === parsed.meta
  );
}

function isInputFocused(): boolean {
  const element = document.activeElement as HTMLElement | null;
  if (!element) {
    return false;
  }

  const tag = element.tagName.toLowerCase();
  if (element.isContentEditable) {
    return true;
  }

  if (tag === 'textarea' || tag === 'select') {
    return true;
  }

  if (tag !== 'input') {
    return false;
  }

  const input = element as HTMLInputElement;
  if (input.disabled || input.readOnly) {
    return false;
  }

  const textEntryTypes = new Set(['text', 'search', 'email', 'password', 'url', 'tel', 'number']);
  return textEntryTypes.has((input.type || 'text').toLowerCase());
}

interface ShortcutEntry {
  id: string;
  parsed: ParsedShortcut;
  action: (context: FileBrowserContext, event: KeyboardEvent) => void | Promise<void>;
  isDisabled?: (context: FileBrowserContext) => boolean;
}

function toShortcutEntry(hotkey: HotkeyConfig): ShortcutEntry {
  return {
    id: hotkey.id,
    parsed: parseShortcut(hotkey.shortcut),
    action: hotkey.action,
    isDisabled: hotkey.isDisabled,
  };
}

function shortcutFingerprint(parsed: ParsedShortcut): string {
  return `${parsed.ctrl ? 'C' : ''}${parsed.shift ? 'S' : ''}${parsed.alt ? 'A' : ''}${parsed.meta ? 'M' : ''}+${parsed.key}`;
}

export interface UseHotkeysOptions {
  fileBrowser: FileBrowserContract;
  hotkeys?: HotkeyConfig[];
  preventShiftTextSelection?: boolean;
  isSuspended?: () => boolean;
}

export function useHotkeys(options: UseHotkeysOptions): void {
  const preventShiftTextSelection = options.preventShiftTextSelection ?? true;

  const shortcutMap = useMemo(() => {
    const hotkeys = options.hotkeys ?? [...defaultHotkeys, ...defaultToolbarHotkeys];
    const map = new Map<string, ShortcutEntry>();
    hotkeys.map(toShortcutEntry).forEach(entry => {
      map.set(shortcutFingerprint(entry.parsed), entry);
    });
    return [...map.values()];
  }, [options.hotkeys]);

  const handleKeyDown = useEffectEvent((event: KeyboardEvent) => {
    if (options.isSuspended?.()) {
      return;
    }

    if (isInputFocused()) {
      return;
    }

    const context: FileBrowserContext = {
      fileBrowser: options.fileBrowser,
    };

    for (const entry of shortcutMap) {
      if (!matchesEvent(entry.parsed, event)) {
        continue;
      }

      if (entry.isDisabled?.(context)) {
        return;
      }

      event.preventDefault();
      void entry.action(context, event);
      return;
    }
  });

  const handleMouseDown = useEffectEvent((event: MouseEvent) => {
    if (event.shiftKey) {
      event.preventDefault();
    }
  });

  useEffect(() => {
    document.addEventListener('keydown', handleKeyDown);

    if (preventShiftTextSelection) {
      document.addEventListener('mousedown', handleMouseDown);
    }

    return () => {
      document.removeEventListener('keydown', handleKeyDown);
      if (preventShiftTextSelection) {
        document.removeEventListener('mousedown', handleMouseDown);
      }
    };
  }, [preventShiftTextSelection]);
}
