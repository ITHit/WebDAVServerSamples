import type { FileBrowserContract } from '@/shared/contracts/fileBrowserContract';
import type { HierarchyItem } from '@/domain/entities/HierarchyItem';

export const ShortcutLabel = {
  Escape: 'Escape',
  Enter: 'Enter',
  ArrowDown: 'ArrowDown',
  ArrowUp: 'ArrowUp',
  AltArrowUp: 'Alt+ArrowUp',
  ShiftArrowDown: 'Shift+ArrowDown',
  ShiftArrowUp: 'Shift+ArrowUp',
  F2: 'F2',
  F5: 'F5',
  Del: 'Del',
  CtrlA: 'Ctrl+A',
  CtrlC: 'Ctrl+C',
  CtrlX: 'Ctrl+X',
  CtrlV: 'Ctrl+V',
} as const;

export type ShortcutLabel = (typeof ShortcutLabel)[keyof typeof ShortcutLabel];

export interface FileBrowserContext {
  fileBrowser: FileBrowserContract;
}

export interface HotkeyConfig {
  id: string;
  shortcut: ShortcutLabel | string;
  description?: string;
  action: (context: FileBrowserContext, event: KeyboardEvent) => void | Promise<void>;
  isDisabled?: (context: FileBrowserContext) => boolean;
}

// ---------------------------------------------------------------------------
// Toolbar button types
// ---------------------------------------------------------------------------
export interface ToolbarButtonLabel {
  text: string;
  show?: boolean;
  class?: string;
}

export interface ToolbarButtonConfig {
  id: string;
  label: ToolbarButtonLabel;
  icon: string;
  shortcutInfo?: ShortcutLabel;
  /** If set, renders as a <label for="..."> instead of a button. */
  inputFor?: string;
  action: (context: FileBrowserContext) => void | Promise<void>;
  isVisible?: (context: FileBrowserContext) => boolean;
  isDisabled?: (context: FileBrowserContext) => boolean;
}

export interface ResolvedToolbarButton {
  id: string;
  label: ToolbarButtonLabel;
  icon: string;
  shortcutInfo?: ShortcutLabel;
  inputFor?: string;
  disabled: boolean;
  action: (context: FileBrowserContext) => void | Promise<void>;
}

// ---------------------------------------------------------------------------
// Context menu types
// ---------------------------------------------------------------------------
export type ContextMenuItemType = 'item' | 'separator';

interface ContextMenuItemBase {
  id: string;
  type?: ContextMenuItemType;
  label?: string;
  icon?: string;
  shortcutInfo?: ShortcutLabel;
  danger?: boolean;
}

export interface ContextMenuItemConfig extends ContextMenuItemBase {
  action?: (context: FileBrowserContext, item: HierarchyItem) => void | Promise<void>;
  isVisible?: (context: FileBrowserContext, item: HierarchyItem) => boolean;
  isDisabled?: (context: FileBrowserContext, item: HierarchyItem) => boolean;
}

export interface ContainerContextMenuItemConfig extends ContextMenuItemBase {
  action?: (context: FileBrowserContext) => void | Promise<void>;
  isVisible?: (context: FileBrowserContext) => boolean;
  isDisabled?: (context: FileBrowserContext) => boolean;
}

export type ContextMenuItemConfigBase = ContextMenuItemConfig | ContainerContextMenuItemConfig;

export interface ResolvedContextMenuItem {
  id: string;
  label?: string;
  icon?: string;
  shortcutInfo?: ShortcutLabel;
  disabled?: boolean;
  danger?: boolean;
  type?: ContextMenuItemType;
  action?: () => void;
}

// ---------------------------------------------------------------------------
// Row toolbar types
// ---------------------------------------------------------------------------
export interface RowToolbarItemConfig {
  id: string;
  title: string;
  icon: string;
  label?: string;
  action: (item: HierarchyItem, context: FileBrowserContext) => void | Promise<void>;
  isVisible?: (item: HierarchyItem, context: FileBrowserContext) => boolean;
  isDisabled?: (item: HierarchyItem, context: FileBrowserContext) => boolean;
}

export interface ResolvedRowToolbarItem {
  id: string;
  title: string;
  icon: string;
  label?: string;
  disabled: boolean;
  action: () => void | Promise<void>;
}
