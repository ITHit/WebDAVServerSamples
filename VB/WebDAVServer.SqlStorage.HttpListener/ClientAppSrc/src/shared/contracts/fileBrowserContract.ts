import type { HierarchyItem } from '@/domain/entities/HierarchyItem';

export interface FileBrowserContract {
  selectedItems: HierarchyItem[];
  storedItems: HierarchyItem[];
  loadFolder(path: string, showSkeleton?: boolean): Promise<void>;
  createFolderWithModal?(): Promise<void>;
  renameItemWithModal(item?: HierarchyItem): Promise<void>;
  deleteItemsWithConfirmation(items?: HierarchyItem[]): Promise<void>;
  downloadItems?(items?: HierarchyItem[]): void;
  editItem?(item?: HierarchyItem): void;
  editItemWith?(item?: HierarchyItem): void;
  lockFiles?(items?: HierarchyItem[]): void | Promise<void>;
  unlockFiles?(items?: HierarchyItem[]): void | Promise<void>;
  printItems?(items?: HierarchyItem[]): void;
  copyItemsToClipboard(items?: HierarchyItem[]): void | Promise<void>;
  cutItemsToClipboard(items?: HierarchyItem[]): void | Promise<void>;
  pasteStoredItems(): Promise<void>;
  openFolderInOsFileManager(folderPath: string): void;
  refresh(): Promise<void>;
  clearSelection(): void;
  selectAll(): void;
  moveSelection(offset: 1 | -1, extend?: boolean): void;
}
