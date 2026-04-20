import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { isFolderItem, isFileItem } from '@/domain/entities/HierarchyItem';
import type { FileBrowserContext } from '@/shared/config/config-types';

export const helpers = {
  hasStoredItems: (ctx: FileBrowserContext) => ctx.fileBrowser.storedItems.length > 0,
  effectiveItem: (ctx: FileBrowserContext): HierarchyItem | null =>
    ctx.fileBrowser.selectedItems.length === 1 ? ctx.fileBrowser.selectedItems[0] : null,
  hasSelection: (ctx: FileBrowserContext) => ctx.fileBrowser.selectedItems.length > 0,
  hasFiles: (ctx: FileBrowserContext) =>
    ctx.fileBrowser.selectedItems.some(item => isFileItem(item)),
  hasFolders: (ctx: FileBrowserContext) =>
    ctx.fileBrowser.selectedItems.some(item => isFolderItem(item)),
  allLocked: (ctx: FileBrowserContext) =>
    ctx.fileBrowser.selectedItems.length > 0 &&
    ctx.fileBrowser.selectedItems.every(item => item.locks.length > 0),
  allUnlocked: (ctx: FileBrowserContext) =>
    ctx.fileBrowser.selectedItems.length > 0 &&
    ctx.fileBrowser.selectedItems.every(item => item.locks.length === 0),
};
