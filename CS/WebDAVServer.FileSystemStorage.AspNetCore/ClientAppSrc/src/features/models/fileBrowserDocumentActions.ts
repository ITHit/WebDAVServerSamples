import { HierarchyItem, isFolderItem } from '@/domain/entities/HierarchyItem';

export type ManageDocumentOperation = 'Edit' | 'Print' | 'Lock' | 'Unlock' | 'OpenWith';

export function getDocumentUrls(items: HierarchyItem[]): string[] {
  return items
    .filter((item) => !isFolderItem(item))
    .map((item) => item.path);
}

export function getSingleDocumentUrl(item?: HierarchyItem): string | null {
  if (!item || isFolderItem(item)) {
    return null;
  }

  return item.path;
}
