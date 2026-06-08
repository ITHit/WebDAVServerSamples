import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import type { ReadonlyBox } from '@/shared/types/box';

/**
 * Minimal UI-facing contract used by shared toolbar/context-menu/hotkey modules.
 * This keeps shared layer independent from the concrete useFileBrowser composable.
 */
export interface FileBrowserContract {
  selectedItems: ReadonlyBox<HierarchyItem[]>;
  storedItems: ReadonlyBox<HierarchyItem[]>;
  isDavProtocolSupported: boolean;

  loadFolder(path: string, showSkeleton?: boolean): Promise<void>;
  editItem(item: HierarchyItem): Promise<void>;
  editItemWith(item: HierarchyItem): Promise<void>;
  createFolderWithModal(): Promise<void>;
  renameItemWithModal(item: HierarchyItem): Promise<void>;
  deleteItemsWithConfirmation(items: HierarchyItem[]): Promise<void>;
  downloadItems(items?: HierarchyItem[]): Promise<void>;
  copyItemsToClipboard(items: HierarchyItem[]): Promise<void>;
  cutItemsToClipboard(items: HierarchyItem[]): Promise<void>;
  pasteStoredItems(): Promise<void>;
  reloadFolder(): Promise<void>;
  printItems(items?: HierarchyItem[]): Promise<void>;
  lockFiles(items: HierarchyItem[]): Promise<void>;
  unlockFiles(items: HierarchyItem[]): Promise<void>;

  clearSelection(): void;
  selectAll(): void;
  moveSelection(offset: number, extend: boolean): void;
}
