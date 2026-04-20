import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { StoredType } from '@/domain/value-objects/StoredType';
import { FileBrowserCoreState } from '@/features/models/fileBrowserCore';
import { FileBrowserUiPorts } from '@/features/models/fileBrowserUiPorts';

function resolveActionItems(items: HierarchyItem[] | undefined, selectedItems: HierarchyItem[]): HierarchyItem[] {
  return items ?? selectedItems;
}

export function storeItemsToClipboardCore(
  state: FileBrowserCoreState,
  items: HierarchyItem[] | undefined,
  selectedItems: HierarchyItem[],
  type: StoredType
): void {
  const itemsToStore = resolveActionItems(items, selectedItems);

  if (itemsToStore.length === 0) {
    return;
  }

  state.storedItems.value = itemsToStore;
  state.storedType.value = type;
}

export async function deleteItemsWithConfirmationCore(
  items: HierarchyItem[] | undefined,
  selectedItems: HierarchyItem[],
  confirmDelete: () => Promise<boolean>,
  deleteByPaths: (itemPaths: string[]) => Promise<void>
): Promise<void> {
  const itemsToDelete = resolveActionItems(items, selectedItems);

  if (itemsToDelete.length === 0) {
    return;
  }

  const confirmed = await confirmDelete();

  if (!confirmed) {
    return;
  }

  const itemPaths = itemsToDelete.map((item) => item.path);
  await deleteByPaths(itemPaths);
}

export async function downloadItemsCore(
  items: HierarchyItem[] | undefined,
  selectedItems: HierarchyItem[],
  downloadByUrls: (urls: string[]) => Promise<void>
): Promise<void> {
  const itemsToDownload = resolveActionItems(items, selectedItems);

  if (itemsToDownload.length === 0) {
    return;
  }

  const urls = itemsToDownload.map((item) => item.path);
  await downloadByUrls(urls);
}

export async function createFolderWithDialogCore(
  uiPorts: FileBrowserUiPorts,
  createFolder: (folderName: string) => Promise<void>
): Promise<void> {
  await uiPorts.showCreateFolderDialog(createFolder);
}

export async function renameItemWithDialogCore(
  item: HierarchyItem | undefined,
  selectedItems: HierarchyItem[],
  uiPorts: FileBrowserUiPorts,
  renameItemByPath: (itemPath: string, newName: string) => Promise<void>
): Promise<void> {
  const itemToRename = item ?? selectedItems[0];

  if (!itemToRename) {
    return;
  }

  await uiPorts.showRenameItemDialog(itemToRename, async (newName: string) => {
    await renameItemByPath(itemToRename.path, newName);
  });
}
