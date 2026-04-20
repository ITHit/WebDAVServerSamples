import { isFolderItem } from '@/domain/entities/HierarchyItem';
import type { RowToolbarItemConfig } from './config-types';

export const defaultRowToolbarItems: RowToolbarItemConfig[] = [
  {
    id: 'open',
    title: 'phrases.rowToolbar.open',
    icon: 'icon-folder',
    action: (item, ctx) => ctx.fileBrowser.openFolderInOsFileManager(item.path),
    isVisible: (item) => isFolderItem(item),
  },
  {
    id: 'edit',
    title: 'phrases.rowToolbar.edit',
    icon: 'icon-edit',
    action: (item, ctx) => ctx.fileBrowser.editItem?.(item),
    isVisible: (item) => !isFolderItem(item),
  },
  {
    id: 'rename',
    title: 'phrases.rowToolbar.rename',
    icon: 'icon-rename-item',
    action: (item, ctx) => ctx.fileBrowser.renameItemWithModal(item),
  },
];
