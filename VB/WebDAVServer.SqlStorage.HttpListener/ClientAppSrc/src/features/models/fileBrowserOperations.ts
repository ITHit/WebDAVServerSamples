import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { IFileSystemRepository } from '@/domain/repositories/IFileSystemRepository';
import { StoredType } from '@/domain/value-objects/StoredType';
import {
  changeSortCore,
  clearSearchCore,
  clearStoredItemsCore,
  copyItemsCore,
  createFolderCore,
  deleteItemsCore,
  FileBrowserCoreState,
  loadFolderCore,
  moveItemsCore,
  nextPageCore,
  pasteStoredItemsCore,
  previousPageCore,
  refreshCore,
  renameItemCore,
  searchCore,
  storeItemsCore,
  updateItemCore,
} from '@/features/models/fileBrowserCore';
import {
  editItemCore,
  editItemWithCore,
  lockItemsCore,
  ManageDocumentsFn,
  printItemsCore,
  unlockItemsCore,
} from '@/features/models/fileBrowserDocumentCommands';
import { FileBrowserUiPorts } from '@/features/models/fileBrowserUiPorts';
import {
  createFolderWithDialogCore,
  deleteItemsWithConfirmationCore,
  downloadItemsCore,
  renameItemWithDialogCore,
  storeItemsToClipboardCore,
} from '@/features/models/fileBrowserUserActions';
import {
  createSelectionController,
} from '@/features/models/fileBrowserSelectionController';

export interface FileBrowserOperationsDeps {
  state: FileBrowserCoreState;
  fileSystemRepository: IFileSystemRepository;
  uiPorts: FileBrowserUiPorts;
  downloadMultipleFiles: (urls: string[]) => Promise<void>;
  manageDocuments: ManageDocumentsFn;
  isDavProtocolSupported: () => boolean;
  getSelectedItems: () => HierarchyItem[];
  getAppPathFromServerUrl: (serverUrl: string) => string;
  getServerUrl: (pathName: string) => string;
}

export function createFileBrowserOperations(deps: FileBrowserOperationsDeps) {
  const {
    state,
    fileSystemRepository,
    uiPorts,
    downloadMultipleFiles,
    manageDocuments,
    isDavProtocolSupported,
    getSelectedItems,
    getAppPathFromServerUrl,
    getServerUrl,
  } = deps;

  const {
    toggleSelection,
    selectSingle,
    setRangeFromAnchor,
    selectAll,
    clearSelection,
    toggleSelectAll,
    moveSelection,
  } = createSelectionController({
    selectedIndexes: state.selectedIndexes,
    selectionAnchor: state.selectionAnchor,
    getItemCount: () => state.items.value.length,
  });

  async function loadFolder(path: string, showSkeleton = false) {
    await loadFolderCore(state, fileSystemRepository, path, showSkeleton);
  }

  async function loadParentFolder(showSkeleton = true) {
    const currentPath = state.currentFolder.value?.path;
    if (!currentPath) return;

    const segments = getAppPathFromServerUrl(currentPath).split('/').filter(Boolean);
    if (segments.length === 0) return;

    const parentAppPath = segments.length === 1 ? '/' : `/${segments.slice(0, -1).join('/')}`;
    await loadFolder(getServerUrl(parentAppPath), showSkeleton);
  }

  async function search(query: string, showSkeleton = false) {
    await searchCore(state, fileSystemRepository, query, showSkeleton);
  }

  async function clearSearch() {
    await clearSearchCore(state, fileSystemRepository);
  }

  async function refresh(showSkeleton = false, loadToCurrentPage = false) {
    await refreshCore(state, fileSystemRepository, showSkeleton, loadToCurrentPage);
  }

  async function changeSort(column: string) {
    await changeSortCore(state, fileSystemRepository, column);
  }

  async function nextPage() {
    await nextPageCore(state, fileSystemRepository);
  }

  async function previousPage() {
    await previousPageCore(state, fileSystemRepository);
  }

  async function createFolder(folderName: string) {
    await createFolderCore(state, fileSystemRepository, folderName);
  }

  async function renameItem(itemPath: string, newName: string) {
    await renameItemCore(state, fileSystemRepository, itemPath, newName);
  }

  async function deleteItems(itemPaths: string[]) {
    await deleteItemsCore(state, fileSystemRepository, itemPaths);
  }

  async function moveItems(itemPaths: string[], targetPath: string) {
    await moveItemsCore(state, fileSystemRepository, itemPaths, targetPath, {
      onMoveConflict: async (error, destinationPath) => {
        await uiPorts.showCopyConflictDialog(error.conflictingNames, async () => {
          await fileSystemRepository.moveItems(error.conflictingPaths, destinationPath, true);
        });
      },
    });
  }

  async function copyItems(itemPaths: string[], targetPath: string) {
    await copyItemsCore(state, fileSystemRepository, itemPaths, targetPath);
  }

  function storeItems(itemsToStore: HierarchyItem[], type: StoredType) {
    storeItemsCore(state, itemsToStore, type);
  }

  function clearStoredItems() {
    clearStoredItemsCore(state);
  }

  async function pasteStoredItems() {
    await pasteStoredItemsCore(state, fileSystemRepository, {
      onCopyConflict: async (error, targetPath) => {
        await uiPorts.showCopyConflictDialog(error.conflictingNames, async () => {
          await fileSystemRepository.copyItems(error.conflictingPaths, targetPath, true);
        });
      },
      onMoveConflict: async (error, targetPath) => {
        await uiPorts.showCopyConflictDialog(error.conflictingNames, async () => {
          await fileSystemRepository.moveItems(error.conflictingPaths, targetPath, true);
        });
      },
    });
  }

  async function createFolderWithModal(): Promise<void> {
    await createFolderWithDialogCore(uiPorts, async (folderName: string) => {
      await createFolder(folderName);
    });
  }

  async function renameItemWithModal(item?: HierarchyItem): Promise<void> {
    await renameItemWithDialogCore(item, getSelectedItems(), uiPorts, async (itemPath, newName) => {
      await renameItem(itemPath, newName);
    });
  }

  async function deleteItemsWithConfirmation(items?: HierarchyItem[]): Promise<void> {
    await deleteItemsWithConfirmationCore(
      items,
      getSelectedItems(),
      async () => uiPorts.confirmDelete(),
      async (itemPaths) => {
        await deleteItems(itemPaths);
      }
    );
  }

  async function downloadItems(items?: HierarchyItem[]): Promise<void> {
    await downloadItemsCore(items, getSelectedItems(), async (urls) => {
      await downloadMultipleFiles(urls);
    });
  }

  function copyItemsToClipboard(items?: HierarchyItem[]): void {
    storeItemsToClipboardCore(state, items, getSelectedItems(), StoredType.Copy);
  }

  function cutItemsToClipboard(items?: HierarchyItem[]): void {
    storeItemsToClipboardCore(state, items, getSelectedItems(), StoredType.Cut);
  }

  function printItems(): void {
    printItemsCore(getSelectedItems(), manageDocuments);
  }

  function editItem(item?: HierarchyItem): void {
    editItemCore(item, getSelectedItems(), manageDocuments);
  }

  function editItemWith(item?: HierarchyItem): void {
    editItemWithCore(item, getSelectedItems(), manageDocuments);
  }

  async function reloadFolder(): Promise<void> {
    await refresh(false);
  }

  function openFolderInOsFileManager(folderPath: string): void {
    uiPorts.openFolderInOsFileManager(folderPath);
  }

  function lockFiles(items?: HierarchyItem[]): void {
    lockItemsCore(items, getSelectedItems(), manageDocuments);
  }

  function unlockFiles(items?: HierarchyItem[]): void {
    unlockItemsCore(items, getSelectedItems(), manageDocuments);
  }

  function isDavProtocolSupportedByClient(): boolean {
    return isDavProtocolSupported();
  }

  async function updateItem(path: string): Promise<void> {
    await updateItemCore(state, fileSystemRepository, path);
  }

  return {
    toggleSelection,
    selectSingle,
    setRangeFromAnchor,
    selectAll,
    clearSelection,
    toggleSelectAll,
    moveSelection,
    loadFolder,
    loadParentFolder,
    search,
    clearSearch,
    refresh,
    changeSort,
    nextPage,
    previousPage,
    createFolder,
    renameItem,
    deleteItems,
    moveItems,
    copyItems,
    storeItems,
    clearStoredItems,
    pasteStoredItems,
    createFolderWithModal,
    renameItemWithModal,
    deleteItemsWithConfirmation,
    downloadItems,
    copyItemsToClipboard,
    cutItemsToClipboard,
    printItems,
    editItem,
    editItemWith,
    reloadFolder,
    openFolderInOsFileManager,
    lockFiles,
    unlockFiles,
    isDavProtocolSupported: isDavProtocolSupportedByClient,
    updateItem,
  };
}
