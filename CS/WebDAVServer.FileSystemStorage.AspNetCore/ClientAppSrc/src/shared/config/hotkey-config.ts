import { isFolderItem } from '@/domain/entities/HierarchyItem';
import type { HotkeyConfig } from '@/shared/config/config-types';
import { ShortcutLabel } from '@/shared/config/config-types';
import { helpers } from '@/shared/config/config-helpers';

export const defaultHotkeys: HotkeyConfig[] = [
  {
    id: 'nav-escape',
    shortcut: ShortcutLabel.Escape,
    description: 'phrases.hotkeys.descriptions.navEscape',
    action: ctx => ctx.fileBrowser.clearSelection(),
  },
  {
    id: 'nav-select-all',
    shortcut: ShortcutLabel.CtrlA,
    description: 'phrases.hotkeys.descriptions.navSelectAll',
    action: ctx => ctx.fileBrowser.selectAll(),
  },
  {
    id: 'nav-arrow-down',
    shortcut: ShortcutLabel.ArrowDown,
    description: 'phrases.hotkeys.descriptions.navArrowDown',
    action: ctx => ctx.fileBrowser.moveSelection(1, false),
  },
  {
    id: 'nav-arrow-down-shift',
    shortcut: ShortcutLabel.ShiftArrowDown,
    description: 'phrases.hotkeys.descriptions.navArrowDownShift',
    action: ctx => ctx.fileBrowser.moveSelection(1, true),
  },
  {
    id: 'nav-arrow-up',
    shortcut: ShortcutLabel.ArrowUp,
    description: 'phrases.hotkeys.descriptions.navArrowUp',
    action: ctx => ctx.fileBrowser.moveSelection(-1, false),
  },
  {
    id: 'nav-arrow-up-shift',
    shortcut: ShortcutLabel.ShiftArrowUp,
    description: 'phrases.hotkeys.descriptions.navArrowUpShift',
    action: ctx => ctx.fileBrowser.moveSelection(-1, true),
  },
  {
    id: 'nav-parent-folder',
    shortcut: ShortcutLabel.AltArrowUp,
    description: 'phrases.hotkeys.descriptions.navParentFolder',
    action: ctx => ctx.fileBrowser.loadParentFolder(true),
  },
  {
    id: 'nav-enter',
    shortcut: ShortcutLabel.Enter,
    description: 'phrases.hotkeys.descriptions.navEnter',
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
    description: 'phrases.hotkeys.descriptions.toolbarRename',
    action: ctx => {
      const item = helpers.effectiveItem(ctx);
      return item ? ctx.fileBrowser.renameItemWithModal(item) : Promise.resolve();
    },
    isDisabled: ctx => !helpers.effectiveItem(ctx),
  },
  {
    id: 'toolbar-copy',
    shortcut: ShortcutLabel.CtrlC,
    description: 'phrases.hotkeys.descriptions.toolbarCopy',
    action: ctx => ctx.fileBrowser.copyItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
  {
    id: 'toolbar-cut',
    shortcut: ShortcutLabel.CtrlX,
    description: 'phrases.hotkeys.descriptions.toolbarCut',
    action: ctx => ctx.fileBrowser.cutItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
  {
    id: 'toolbar-paste',
    shortcut: ShortcutLabel.CtrlV,
    description: 'phrases.hotkeys.descriptions.toolbarPaste',
    action: ctx => ctx.fileBrowser.pasteStoredItems(),
    isDisabled: ctx => !helpers.hasStoredItems(ctx),
  },
  {
    id: 'toolbar-reload',
    shortcut: ShortcutLabel.F5,
    description: 'phrases.hotkeys.descriptions.toolbarReload',
    action: ctx => ctx.fileBrowser.refresh(),
  },
  {
    id: 'toolbar-delete',
    shortcut: ShortcutLabel.Del,
    description: 'phrases.hotkeys.descriptions.toolbarDelete',
    action: ctx => ctx.fileBrowser.deleteItemsWithConfirmation(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
];
