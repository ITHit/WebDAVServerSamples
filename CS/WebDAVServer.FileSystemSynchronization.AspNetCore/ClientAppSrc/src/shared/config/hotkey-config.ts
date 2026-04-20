import { isFolderItem } from '@/domain/entities/HierarchyItem';
import type { HotkeyConfig } from '@/shared/config/config-types';
import { ShortcutLabel } from '@/shared/config/config-types';
import { helpers } from '@/shared/config/config-helpers';

export const defaultHotkeys: HotkeyConfig[] = [
  {
    id: 'nav-escape',
    shortcut: ShortcutLabel.Escape,
    description: 'Clear selection',
    action: ctx => ctx.fileBrowser.clearSelection(),
  },
  {
    id: 'nav-select-all',
    shortcut: ShortcutLabel.CtrlA,
    description: 'Select all items',
    action: ctx => ctx.fileBrowser.selectAll(),
  },
  {
    id: 'nav-arrow-down',
    shortcut: ShortcutLabel.ArrowDown,
    description: 'Move selection down',
    action: ctx => ctx.fileBrowser.moveSelection(1, false),
  },
  {
    id: 'nav-arrow-down-shift',
    shortcut: ShortcutLabel.ShiftArrowDown,
    description: 'Extend selection down',
    action: ctx => ctx.fileBrowser.moveSelection(1, true),
  },
  {
    id: 'nav-arrow-up',
    shortcut: ShortcutLabel.ArrowUp,
    description: 'Move selection up',
    action: ctx => ctx.fileBrowser.moveSelection(-1, false),
  },
  {
    id: 'nav-arrow-up-shift',
    shortcut: ShortcutLabel.ShiftArrowUp,
    description: 'Extend selection up',
    action: ctx => ctx.fileBrowser.moveSelection(-1, true),
  },
  {
    id: 'nav-enter',
    shortcut: ShortcutLabel.Enter,
    description: 'Open selected folder',
    action: ctx => {
      const selected = ctx.fileBrowser.selectedItems;
      if (selected.length === 1 && isFolderItem(selected[0])) {
        return ctx.fileBrowser.loadFolder(selected[0].path, true);
      }

      return undefined;
    },
  },
];

export const defaultToolbarHotkeys: HotkeyConfig[] = [
  {
    id: 'toolbar-rename',
    shortcut: ShortcutLabel.F2,
    description: 'Rename selected item',
    action: ctx => {
      const item = helpers.effectiveItem(ctx);
      return item ? ctx.fileBrowser.renameItemWithModal(item) : Promise.resolve();
    },
    isDisabled: ctx => !helpers.effectiveItem(ctx),
  },
  {
    id: 'toolbar-copy',
    shortcut: ShortcutLabel.CtrlC,
    description: 'Copy selected items',
    action: ctx => ctx.fileBrowser.copyItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
  {
    id: 'toolbar-cut',
    shortcut: ShortcutLabel.CtrlX,
    description: 'Cut selected items',
    action: ctx => ctx.fileBrowser.cutItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
  {
    id: 'toolbar-paste',
    shortcut: ShortcutLabel.CtrlV,
    description: 'Paste clipboard items',
    action: ctx => ctx.fileBrowser.pasteStoredItems(),
    isDisabled: ctx => !helpers.hasStoredItems(ctx),
  },
  {
    id: 'toolbar-reload',
    shortcut: ShortcutLabel.F5,
    description: 'Refresh current folder',
    action: ctx => ctx.fileBrowser.refresh(),
  },
  {
    id: 'toolbar-delete',
    shortcut: ShortcutLabel.Del,
    description: 'Delete selected items',
    action: ctx => ctx.fileBrowser.deleteItemsWithConfirmation(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
];
