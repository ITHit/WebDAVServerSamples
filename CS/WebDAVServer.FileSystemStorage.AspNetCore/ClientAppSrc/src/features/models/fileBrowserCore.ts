import { FolderItem } from '@/domain/entities/FolderItem';
import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { IFileSystemRepository } from '@/domain/repositories/IFileSystemRepository';
import { PaginationOptions } from '@/domain/value-objects/PaginationOptions';
import { ServerCapabilities } from '@/domain/value-objects/ServerCapabilities';
import { SortOptions } from '@/domain/value-objects/SortOptions';
import { StoredType } from '@/domain/value-objects/StoredType';
import { CopyConflictError } from '@/shared/types/appErrors';
import { ValidationError } from '@/shared/types/appErrors';
import type { MutableBox } from '@/shared/types/box';
import {
  validateCopyItems,
  validateFolderName,
  validateMoveItems,
  validateRenameName,
} from '@/features/models/fileBrowserValidation';

export interface FileBrowserCoreState {
  currentFolder: MutableBox<FolderItem | null>;
  items: MutableBox<HierarchyItem[]>;
  loading: MutableBox<boolean>;
  loadingWithSkeleton: MutableBox<boolean>;
  loadingNextPage: MutableBox<boolean>;
  error: MutableBox<string | null>;
  totalItems: MutableBox<number>;
  currentPage: MutableBox<number>;
  pageSize: MutableBox<number>;
  sortAscending: MutableBox<boolean>;
  sortColumn: MutableBox<string>;
  currentSearchQuery: MutableBox<string | null>;
  serverCapabilities: MutableBox<ServerCapabilities>;
  optionsInfoLoading: MutableBox<boolean>;
  selectedIndexes: MutableBox<number[]>;
  selectionAnchor: MutableBox<number | null>;
  storedItems: MutableBox<HierarchyItem[]>;
  storedType: MutableBox<StoredType>;
}

export interface PasteStoredItemsOptions {
  onCopyConflict?: (error: CopyConflictError, targetPath: string) => Promise<void>;
  onMoveConflict?: (error: CopyConflictError, targetPath: string) => Promise<void>;
}

export interface MoveItemsOptions {
  onMoveConflict?: (error: CopyConflictError, targetPath: string) => Promise<void>;
}

function clearSelection(state: FileBrowserCoreState): void {
  state.selectedIndexes.value = [];
  state.selectionAnchor.value = null;
}

function getCountPages(state: FileBrowserCoreState): number {
  return Math.ceil(state.totalItems.value / state.pageSize.value);
}

function hasNextPage(state: FileBrowserCoreState): boolean {
  return state.currentPage.value < getCountPages(state);
}

async function fetchPage(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  folderPath: string,
  pagination: PaginationOptions,
  sort: SortOptions,
  append: boolean
): Promise<void> {
  const result = state.currentSearchQuery.value
    ? await fileSystemRepository.searchInFolder(folderPath, state.currentSearchQuery.value, pagination)
    : await fileSystemRepository.getFolderContents(folderPath, pagination, sort);

  state.currentFolder.value = result.folder;

  if (append) {
    const existingPaths = new Set(state.items.value.map((item) => item.path));
    state.items.value = [...state.items.value, ...result.items.filter((item) => !existingPaths.has(item.path))];
  } else {
    state.items.value = result.items;
    clearSelection(state);
  }

  state.totalItems.value = result.totalItems;
}

export async function loadFolderCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  path: string,
  showSkeleton = false
): Promise<void> {
  state.currentSearchQuery.value = null;

  const isFolderChanged = state.currentFolder.value?.path !== path;
  if (isFolderChanged) {
    state.currentPage.value = 1;
  }

  if (showSkeleton) {
    state.loadingWithSkeleton.value = true;
  }

  state.loading.value = true;
  state.error.value = null;

  try {
    const pagination = new PaginationOptions(state.currentPage.value, state.pageSize.value);
    const sort = new SortOptions(state.sortColumn.value, state.sortAscending.value);

    await fetchPage(state, fileSystemRepository, path, pagination, sort, false);

    if (state.optionsInfoLoading.value && state.currentFolder.value) {
      try {
        state.serverCapabilities.value = await fileSystemRepository.getSupportedFeatures(state.currentFolder.value.path);
        state.optionsInfoLoading.value = false;
      } catch (err) {
        console.error('Failed to load server capabilities:', err);
      }
    }
  } catch (err) {
    state.error.value = err instanceof Error ? err.message : 'Failed to load folder contents.';
    throw err;
  } finally {
    state.loading.value = false;
    state.loadingWithSkeleton.value = false;
  }
}

export async function searchCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  query: string,
  showSkeleton = false
): Promise<void> {
  if (!state.currentFolder.value) {
    return;
  }

  state.currentSearchQuery.value = query;
  state.currentPage.value = 1;

  if (showSkeleton) {
    state.loadingWithSkeleton.value = true;
  }

  state.loading.value = true;
  state.error.value = null;

  try {
    const pagination = new PaginationOptions(1, state.pageSize.value);
    const sort = new SortOptions(state.sortColumn.value, state.sortAscending.value);
    await fetchPage(state, fileSystemRepository, state.currentFolder.value.path, pagination, sort, false);
  } catch (err) {
    state.error.value = err instanceof Error ? err.message : 'Failed to search in folder.';
    throw err;
  } finally {
    state.loading.value = false;
    state.loadingWithSkeleton.value = false;
  }
}

export async function clearSearchCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository
): Promise<void> {
  if (!state.currentSearchQuery.value) {
    return;
  }

  state.currentSearchQuery.value = null;
  await refreshCore(state, fileSystemRepository);
}

export async function changeSortCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  column: string
): Promise<void> {
  if (state.sortColumn.value === column) {
    state.sortAscending.value = !state.sortAscending.value;
  } else {
    state.sortColumn.value = column;
    state.sortAscending.value = true;
  }

  state.currentPage.value = 1;

  if (state.currentFolder.value) {
    await refreshCore(state, fileSystemRepository);
  }
}

export async function refreshCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  showSkeleton = false,
  loadToCurrentPage = false
): Promise<void> {
  if (!state.currentFolder.value) {
    return;
  }

  const path = state.currentFolder.value.path;
  const savedPage = state.currentPage.value;
  state.currentPage.value = 1;

  if (showSkeleton) {
    state.loadingWithSkeleton.value = true;
  }

  state.loading.value = true;
  state.error.value = null;

  try {
    const sort = new SortOptions(state.sortColumn.value, state.sortAscending.value);
    await fetchPage(state, fileSystemRepository, path, new PaginationOptions(1, state.pageSize.value), sort, false);

    if (loadToCurrentPage) {
      for (let page = 2; page <= savedPage; page++) {
        state.currentPage.value = page;
        await fetchPage(state, fileSystemRepository, path, new PaginationOptions(page, state.pageSize.value), sort, true);
      }
    }
  } catch (err) {
    state.error.value = err instanceof Error ? err.message : 'Failed to refresh folder.';
    throw err;
  } finally {
    state.loading.value = false;
    state.loadingWithSkeleton.value = false;
  }
}

export async function nextPageCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository
): Promise<void> {
  if (!hasNextPage(state) || !state.currentFolder.value || state.loadingNextPage.value) {
    return;
  }

  state.loadingNextPage.value = true;
  const previousPage = state.currentPage.value;

  try {
    state.currentPage.value++;
    const pagination = new PaginationOptions(state.currentPage.value, state.pageSize.value);
    const sort = new SortOptions(state.sortColumn.value, state.sortAscending.value);

    await fetchPage(state, fileSystemRepository, state.currentFolder.value.path, pagination, sort, true);
  } catch (err) {
    state.currentPage.value = previousPage;
    throw err;
  } finally {
    state.loadingNextPage.value = false;
  }
}

export async function previousPageCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository
): Promise<void> {
  if (state.currentPage.value <= 1 || !state.currentFolder.value) {
    return;
  }

  state.currentPage.value--;
  await loadFolderCore(state, fileSystemRepository, state.currentFolder.value.path);
}

export async function createFolderCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  folderName: string
): Promise<void> {
  if (!state.currentFolder.value) {
    throw new Error('No current folder');
  }

  const trimmedName = validateFolderName(folderName);
  await fileSystemRepository.createFolder(state.currentFolder.value.path, trimmedName);
  await refreshCore(state, fileSystemRepository);
}

export async function renameItemCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  itemPath: string,
  newName: string
): Promise<void> {
  const trimmedName = validateRenameName(newName);
  await fileSystemRepository.renameItem(itemPath, trimmedName);
  await refreshCore(state, fileSystemRepository);
}

export async function deleteItemsCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  itemPaths: string[]
): Promise<void> {
  if (!itemPaths.length) {
    throw new ValidationError('phrases.validations.noItemsSelectedForDeletion');
  }

  await fileSystemRepository.deleteItems(itemPaths);
  await refreshCore(state, fileSystemRepository);
}

export async function moveItemsCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  itemPaths: string[],
  targetPath: string,
  options: MoveItemsOptions = {}
): Promise<void> {
  validateMoveItems(itemPaths, targetPath);

  try {
    await fileSystemRepository.moveItems(itemPaths, targetPath);
  } catch (error) {
    if (error instanceof CopyConflictError) {
      await refreshCore(state, fileSystemRepository);

      if (!options.onMoveConflict) {
        throw error;
      }

      await options.onMoveConflict(error, targetPath);
      await refreshCore(state, fileSystemRepository);
      return;
    }

    throw error;
  }

  await refreshCore(state, fileSystemRepository);
}

export async function copyItemsCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  itemPaths: string[],
  targetPath: string
): Promise<void> {
  validateCopyItems(itemPaths, targetPath);
  await fileSystemRepository.copyItems(itemPaths, targetPath);
  await refreshCore(state, fileSystemRepository);
}

export function storeItemsCore(
  state: FileBrowserCoreState,
  itemsToStore: HierarchyItem[],
  type: StoredType
): void {
  state.storedItems.value = itemsToStore;
  state.storedType.value = type;
}

export function clearStoredItemsCore(state: FileBrowserCoreState): void {
  state.storedItems.value = [];
}

export async function pasteStoredItemsCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  options: PasteStoredItemsOptions = {}
): Promise<void> {
  if (!state.currentFolder.value || state.storedItems.value.length === 0) {
    return;
  }

  const itemPaths = state.storedItems.value.map((item) => item.path);
  const targetPath = state.currentFolder.value.path;

  if (state.storedType.value === StoredType.Copy) {
    try {
      await fileSystemRepository.copyItems(itemPaths, targetPath);
    } catch (error) {
      if (error instanceof CopyConflictError) {
        await refreshCore(state, fileSystemRepository);

        if (!options.onCopyConflict) {
          throw error;
        }

        await options.onCopyConflict(error, targetPath);
        await refreshCore(state, fileSystemRepository);
        clearStoredItemsCore(state);
        return;
      }

      throw error;
    }

    await refreshCore(state, fileSystemRepository);
  } else {
    await moveItemsCore(state, fileSystemRepository, itemPaths, targetPath, {
      onMoveConflict: options.onMoveConflict,
    });
  }

  clearStoredItemsCore(state);
}

export async function updateItemCore(
  state: FileBrowserCoreState,
  fileSystemRepository: IFileSystemRepository,
  path: string
): Promise<void> {
  const index = state.items.value.findIndex((item) => item.path === path);

  if (index === -1) {
    return;
  }

  try {
    const updated = await fileSystemRepository.getItem(path);
    // React state updates must replace the array reference to trigger a re-render.
    const nextItems = [...state.items.value];
    nextItems[index] = updated;
    state.items.value = nextItems;
  } catch {
    await refreshCore(state, fileSystemRepository, false, true);
  }
}
