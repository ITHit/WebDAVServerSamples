import type { ToolbarButtonConfig } from './config-types';
import { ShortcutLabel } from './config-types';
import { helpers } from './config-helpers';

export const defaultToolbarButtons: ToolbarButtonConfig[] = [
  {
    id: 'create-folder',
    label: { text: 'phrases.toolbar.createFolderButton', show: true, class: 'hidden lg:block' },
    icon: 'icon-create-folder',
    action: ctx => ctx.fileBrowser.createFolderWithModal?.() ?? Promise.resolve(),
  },
  {
    id: 'download',
    label: { text: 'phrases.toolbar.downloadButton', show: true, class: 'hidden lg:block' },
    icon: 'icon-download-items',
    action: ctx => ctx.fileBrowser.downloadItems?.(),
    isDisabled: ctx => !helpers.hasSelection(ctx) || !helpers.hasFiles(ctx),
  },
  {
    id: 'upload',
    label: { text: 'phrases.toolbar.uploadButton', show: true, class: 'hidden lg:block' },
    icon: 'icon-upload-items',
    inputFor: 'ithit-hidden-input',
    action: () => { },
  },
  {
    id: 'rename',
    label: { text: 'phrases.toolbar.renameButton' },
    icon: 'icon-rename-item',
    shortcutInfo: ShortcutLabel.F2,
    action: ctx => {
      const item = helpers.effectiveItem(ctx);
      return item ? ctx.fileBrowser.renameItemWithModal(item) : Promise.resolve();
    },
    isDisabled: ctx => !helpers.effectiveItem(ctx),
  },
  {
    id: 'lock',
    label: { text: 'phrases.toolbar.lockButton' },
    icon: 'icon-lock',
    action: ctx => ctx.fileBrowser.lockFiles?.(ctx.fileBrowser.selectedItems),
    isVisible: ctx => !helpers.allLocked(ctx),
    isDisabled: ctx => !helpers.allUnlocked(ctx),
  },
  {
    id: 'unlock',
    label: { text: 'phrases.toolbar.unlockButton' },
    icon: 'icon-unlock',
    action: ctx => ctx.fileBrowser.unlockFiles?.(ctx.fileBrowser.selectedItems),
    isVisible: ctx => helpers.allLocked(ctx),
    isDisabled: ctx => !helpers.allLocked(ctx),
  },
  {
    id: 'copy',
    label: { text: 'phrases.toolbar.copyButton' },
    icon: 'icon-copy-items',
    shortcutInfo: ShortcutLabel.CtrlC,
    action: ctx => ctx.fileBrowser.copyItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => ctx.fileBrowser.selectedItems.length === 0,
  },
  {
    id: 'cut',
    label: { text: 'phrases.toolbar.cutButton' },
    icon: 'icon-cut-items',
    shortcutInfo: ShortcutLabel.CtrlX,
    action: ctx => ctx.fileBrowser.cutItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => ctx.fileBrowser.selectedItems.length === 0,
  },
  {
    id: 'paste',
    label: { text: 'phrases.toolbar.pasteButton' },
    icon: 'icon-paste-items',
    shortcutInfo: ShortcutLabel.CtrlV,
    action: ctx => ctx.fileBrowser.pasteStoredItems(),
    isDisabled: ctx => !helpers.hasStoredItems(ctx),
  },
  {
    id: 'reload',
    label: { text: 'phrases.toolbar.reloadButton' },
    icon: 'icon-reload-items',
    action: ctx => ctx.fileBrowser.refresh(),
  },
  {
    id: 'delete',
    label: { text: 'phrases.toolbar.deleteButton', show: true, class: 'hidden lg:block' },
    icon: 'icon-delete-items',
    shortcutInfo: ShortcutLabel.Del,
    action: ctx => ctx.fileBrowser.deleteItemsWithConfirmation(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => ctx.fileBrowser.selectedItems.length === 0,
  },
];
