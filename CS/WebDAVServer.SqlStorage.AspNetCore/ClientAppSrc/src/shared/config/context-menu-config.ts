import { isFolderItem } from '@/domain/entities/HierarchyItem';
import type { ContextMenuItemConfig, ContainerContextMenuItemConfig } from './config-types';
import { ShortcutLabel } from './config-types';
import { helpers } from './config-helpers';

export const defaultContainerContextMenuItems: ContainerContextMenuItemConfig[] = [
  {
    id: 'new-folder',
    label: 'phrases.toolbar.createFolderButton',
    icon: 'icon-create-folder',
    action: ctx => ctx.fileBrowser.createFolderWithModal?.(),
  },
];

export const defaultContextMenuItems: ContextMenuItemConfig[] = [
  {
    id: 'open',
    label: 'phrases.contextMenu.open',
    icon: 'icon-folder',
    action: (ctx, item) => ctx.fileBrowser.loadFolder(item.path),
    isVisible: (_ctx, item) => isFolderItem(item),
  },
  {
    id: 'edit',
    label: 'phrases.toolbar.editButton',
    icon: 'icon-edit',
    action: (ctx, item) => ctx.fileBrowser.editItem?.(item),
    isVisible: (_ctx, item) => !isFolderItem(item),
  },
  {
    id: 'editWith',
    label: 'phrases.toolbar.editWithButton',
    icon: 'icon-edit',
    action: (ctx, item) => ctx.fileBrowser.editItemWith?.(item),
    isVisible: (_ctx, item) => !isFolderItem(item),
  },
  {
    id: 'lock',
    label: 'phrases.toolbar.lockButton',
    icon: 'icon-lock',
    action: (ctx, item) => ctx.fileBrowser.lockFiles?.([item]),
    isVisible: (_ctx, item) => item.locks.length === 0 && !isFolderItem(item),
  },
  {
    id: 'unlock',
    label: 'phrases.toolbar.unlockButton',
    icon: 'icon-unlock',
    action: (ctx, item) => ctx.fileBrowser.unlockFiles?.([item]),
    isVisible: (_ctx, item) => item.locks.length > 0 && !isFolderItem(item),
  },
  {
    id: 'download',
    label: 'phrases.toolbar.downloadButton',
    icon: 'icon-download-items',
    action: (ctx, item) => ctx.fileBrowser.downloadItems?.([item]),
    isVisible: (_ctx, item) => !isFolderItem(item),
  },
  {
    id: 'separator-1',
    type: 'separator',
  },
  {
    id: 'rename',
    label: 'phrases.toolbar.renameButton',
    icon: 'icon-rename-item',
    shortcutInfo: ShortcutLabel.F2,
    action: (ctx, item) => ctx.fileBrowser.renameItemWithModal(item),
  },
  {
    id: 'copy',
    label: 'phrases.toolbar.copyButton',
    icon: 'icon-copy-items',
    shortcutInfo: ShortcutLabel.CtrlC,
    action: (ctx, item) => ctx.fileBrowser.copyItemsToClipboard([item]),
  },
  {
    id: 'cut',
    label: 'phrases.toolbar.cutButton',
    icon: 'icon-cut-items',
    shortcutInfo: ShortcutLabel.CtrlX,
    action: (ctx, item) => ctx.fileBrowser.cutItemsToClipboard([item]),
  },
  {
    id: 'separator-2',
    type: 'separator',
  },
  {
    id: 'delete',
    label: 'phrases.toolbar.deleteButton',
    icon: 'icon-delete-items',
    shortcutInfo: ShortcutLabel.Del,
    danger: true,
    action: (ctx, item) => ctx.fileBrowser.deleteItemsWithConfirmation([item]),
  },
];

export const defaultMultiContextMenuItems: ContextMenuItemConfig[] = [
  {
    id: 'download-multi',
    label: 'phrases.toolbar.downloadButton',
    icon: 'icon-download-items',
    action: ctx => ctx.fileBrowser.downloadItems?.(ctx.fileBrowser.selectedItems),
    isVisible: ctx => !helpers.hasFolders(ctx),
    isDisabled: ctx => !helpers.hasSelection(ctx) || !helpers.hasFiles(ctx),
  },
  {
    id: 'separator-1',
    type: 'separator',
  },
  {
    id: 'lock-multi',
    label: 'phrases.toolbar.lockButton',
    icon: 'icon-lock',
    action: ctx => ctx.fileBrowser.lockFiles?.(ctx.fileBrowser.selectedItems),
    isVisible: ctx => !helpers.allLocked(ctx) && !helpers.hasFolders(ctx),
    isDisabled: ctx => !helpers.allUnlocked(ctx),
  },
  {
    id: 'unlock-multi',
    label: 'phrases.toolbar.unlockButton',
    icon: 'icon-unlock',
    action: ctx => ctx.fileBrowser.unlockFiles?.(ctx.fileBrowser.selectedItems),
    isVisible: ctx => helpers.allLocked(ctx) && !helpers.hasFolders(ctx),
    isDisabled: ctx => !helpers.allLocked(ctx),
  },
  {
    id: 'separator-2',
    type: 'separator',
  },
  {
    id: 'copy-multi',
    label: 'phrases.toolbar.copyButton',
    icon: 'icon-copy-items',
    shortcutInfo: ShortcutLabel.CtrlC,
    action: ctx => ctx.fileBrowser.copyItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
  {
    id: 'cut-multi',
    label: 'phrases.toolbar.cutButton',
    icon: 'icon-cut-items',
    shortcutInfo: ShortcutLabel.CtrlX,
    action: ctx => ctx.fileBrowser.cutItemsToClipboard(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
  {
    id: 'separator-3',
    type: 'separator',
  },
  {
    id: 'delete-multi',
    label: 'phrases.toolbar.deleteButton',
    icon: 'icon-delete-items',
    shortcutInfo: ShortcutLabel.Del,
    danger: true,
    action: ctx => ctx.fileBrowser.deleteItemsWithConfirmation(ctx.fileBrowser.selectedItems),
    isDisabled: ctx => !helpers.hasSelection(ctx),
  },
];
