import { useCallback, useMemo, useRef, useState } from 'react';
import { FolderItem } from '@/domain/entities/FolderItem';
import type { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { isFileItem } from '@/domain/entities/HierarchyItem';
import { ServerCapabilities } from '@/domain/value-objects/ServerCapabilities';
import { StoredType } from '@/domain/value-objects/StoredType';
import {
  getSelectedItems,
  hasSelection as hasSelectionState,
  isAllSelected as isAllSelectedState,
} from '@/features/models/fileBrowserSelection';
import { createReactFileBrowserUiPorts } from '@/features/adapters/react/createReactFileBrowserUiPorts';
import { type FileBrowserCoreState } from '@/features/models/fileBrowserCore';
import { createFileBrowserOperations } from '@/features/models/fileBrowserOperations';
import {
  getCountPages,
  hasNextPage as hasNextPageState,
  hasPreviousPage as hasPreviousPageState,
  isSearchMode as isSearchModeState,
} from '@/features/models/fileBrowserViewState';
import { WebDavSettings } from '@/infrastructure/config/webDavSettings';
import { WebDavFileSystemRepository } from '@/infrastructure/repositories/WebDavFileSystemRepository';
import { getAppPathFromServerUrl, getServerRootUrl } from '@/infrastructure/services/webDavBaseUrl';
import { WebDavClient } from '@/infrastructure/webdav/WebDavClient';
import { downloadMultipleFiles } from '@/shared/utils/downloadUtils';
import type { MutableBox } from '@/shared/types/box';
import { PaginationOptions } from '@/domain/value-objects/PaginationOptions';

export type FileBrowserViewModel = {
  title: string;
  rootUrl: string;
  currentFolderPath: string;
  breadcrumbs: Array<{ label: string; path: string }>;
  items: HierarchyItem[];
  loading: boolean;
  loadingWithSkeleton: boolean;
  error: string | null;
  searchQuery: string;
  sortColumn: string;
  sortAscending: boolean;
  currentPage: number;
  pageSize: number;
  totalItems: number;
  countPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  isSearchMode: boolean;
  loadingNextPage: boolean;
  serverCapabilities: ServerCapabilities;
  optionsInfoLoading: boolean;
  storedItems: HierarchyItem[];
  storedType: StoredType;
  hasStoredItems: boolean;
  selectionAnchor: number | null;
  selectedIndexes: number[];
  selectedItems: HierarchyItem[];
  isAllSelected: boolean;
  hasSelection: boolean;
  loadFolder: (path: string, showSkeleton?: boolean) => Promise<void>;
  search: (query: string) => Promise<void>;
  clearSearch: () => Promise<void>;
  toggleSort: (column: string) => Promise<void>;
  nextPage: () => Promise<void>;
  previousPage: () => Promise<void>;
  refresh: (_preservePagination?: boolean, _forceRefresh?: boolean) => Promise<void>;
  setSort: (column: string, ascending: boolean) => void;
  createFolderWithModal: () => Promise<void>;
  renameItemWithModal: (item?: HierarchyItem) => Promise<void>;
  deleteItemsWithConfirmation: (items?: HierarchyItem[]) => Promise<void>;
  copyItemsToClipboard: (items?: HierarchyItem[]) => void;
  cutItemsToClipboard: (items?: HierarchyItem[]) => void;
  pasteStoredItems: () => Promise<void>;
  clearStoredItems: () => void;
  updateItem: (path: string) => Promise<void>;
  printItems: () => void;
  editItem: (item?: HierarchyItem) => void;
  editItemWith: (item?: HierarchyItem) => void;
  openFolderInOsFileManager: (folderPath: string) => void;
  lockFiles: (items?: HierarchyItem[]) => void;
  unlockFiles: (items?: HierarchyItem[]) => void;
  isDavProtocolSupported: boolean;
  hasSingleFileSelection: boolean;
  hasLockableSelection: boolean;
  hasUnlockableSelection: boolean;
  toggleSelection: (index: number) => void;
  selectSingle: (index: number) => void;
  setRangeFromAnchor: (index: number) => void;
  selectAll: () => void;
  clearSelection: () => void;
  toggleSelectAll: () => void;
  moveSelection: (delta: 1 | -1, extend?: boolean) => void;
  searchSuggestions: (query: string) => Promise<HierarchyItem[]>;
};

const DEFAULT_PAGE_SIZE = 50;

function buildBreadcrumbs(folderPath: string) {
  const appPath = getAppPathFromServerUrl(folderPath);
  const segments = appPath.split('/').filter(Boolean);

  const items: Array<{ label: string; path: string }> = [{ label: '', path: '/' }];
  let accumulatedPath = '';

  segments.forEach(segment => {
    accumulatedPath += `/${segment}`;
    items.push({ label: decodeURIComponent(segment), path: accumulatedPath });
  });

  return items;
}

export function useFileBrowser(): FileBrowserViewModel {
  const rootUrl = useMemo(() => getServerRootUrl(), []);
  const webDavClient = useMemo(() => new WebDavClient(), []);
  const repository = useMemo(() => new WebDavFileSystemRepository(webDavClient), [webDavClient]);
  const openFolderInOsFileManagerPort = useCallback(
    (folderPath: string, showProtocolInstallModal: () => void) => {
      WebDavClient.openFolderInOsFileManager(
        folderPath,
        WebDavSettings.WebsiteRootUrl,
        showProtocolInstallModal,
        null,
        WebDavSettings.EditDocAuth.SearchIn ?? '',
        WebDavSettings.EditDocAuth.CookieNames ?? '',
        WebDavSettings.EditDocAuth.LoginUrl ?? ''
      );
    },
    []
  );
  const uiPorts = useMemo(
    () => createReactFileBrowserUiPorts(openFolderInOsFileManagerPort),
    [openFolderInOsFileManagerPort]
  );

  const [currentFolderPath, setCurrentFolderPath] = useState(rootUrl);
  const [currentFolder, setCurrentFolder] = useState<FolderItem | null>(null);
  const [items, setItems] = useState<HierarchyItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [loadingWithSkeleton, setLoadingWithSkeleton] = useState(false);
  const [loadingNextPage, setLoadingNextPage] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentSearchQuery, setCurrentSearchQuery] = useState<string | null>(null);
  const [sortColumn, setSortColumn] = useState('displayname');
  const [sortAscending, setSortAscending] = useState(true);
  const [totalItems, setTotalItems] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(DEFAULT_PAGE_SIZE);
  const [serverCapabilities, setServerCapabilities] = useState(ServerCapabilities.createEmpty());
  const [optionsInfoLoading, setOptionsInfoLoading] = useState(true);
  const [storedItems, setStoredItems] = useState<HierarchyItem[]>([]);
  const [storedType, setStoredType] = useState<StoredType>(StoredType.Copy);
  const [selectionAnchor, setSelectionAnchor] = useState<number | null>(null);
  const [selectedIndexes, setSelectedIndexes] = useState<number[]>([]);

  // Store state setters in refs so they're always consistent
  const stateSettersRef = useRef({
    setCurrentFolder,
    setCurrentFolderPath,
    setItems,
    setLoading,
    setLoadingWithSkeleton,
    setLoadingNextPage,
    setError,
    setCurrentSearchQuery,
    setSortColumn,
    setSortAscending,
    setTotalItems,
    setCurrentPage,
    setServerCapabilities,
    setOptionsInfoLoading,
    setStoredItems,
    setStoredType,
    setSelectionAnchor,
    setSelectedIndexes,
  });

  // stateRef always reflects the latest state so box getters never capture stale closures.
  // It is updated synchronously on every render before boxes are accessed.
  const stateRef = useRef({
    selectedIndexes,
    selectionAnchor,
    currentFolder,
    items,
    loading,
    loadingWithSkeleton,
    loadingNextPage,
    error,
    totalItems,
    currentPage,
    pageSize,
    serverCapabilities,
    optionsInfoLoading,
    sortAscending,
    sortColumn,
    currentSearchQuery,
    storedItems,
    storedType,
  });
  stateRef.current = {
    selectedIndexes,
    selectionAnchor,
    currentFolder,
    items,
    loading,
    loadingWithSkeleton,
    loadingNextPage,
    error,
    totalItems,
    currentPage,
    pageSize,
    serverCapabilities,
    optionsInfoLoading,
    sortAscending,
    sortColumn,
    currentSearchQuery,
    storedItems,
    storedType,
  };

  // Create boxes once and store in ref
  const boxesRef = useRef<{
    selectedIndexesBox: MutableBox<number[]>;
    selectionAnchorBox: MutableBox<number | null>;
    currentFolderBox: MutableBox<FolderItem | null>;
    itemsBox: MutableBox<HierarchyItem[]>;
    loadingBox: MutableBox<boolean>;
    loadingWithSkeletonBox: MutableBox<boolean>;
    loadingNextPageBox: MutableBox<boolean>;
    errorBox: MutableBox<string | null>;
    totalItemsBox: MutableBox<number>;
    currentPageBox: MutableBox<number>;
    pageSizeBox: MutableBox<number>;
    serverCapabilitiesBox: MutableBox<ServerCapabilities>;
    optionsInfoLoadingBox: MutableBox<boolean>;
    sortAscendingBox: MutableBox<boolean>;
    sortColumnBox: MutableBox<string>;
    currentSearchQueryBox: MutableBox<string | null>;
    storedItemsBox: MutableBox<HierarchyItem[]>;
    storedTypeBox: MutableBox<StoredType>;
  } | null>(null);

  if (!boxesRef.current) {
    boxesRef.current = {
      selectedIndexesBox: {
        get value() { return stateRef.current.selectedIndexes; },
        set value(nextValue: number[]) { stateSettersRef.current.setSelectedIndexes(nextValue); },
      },
      selectionAnchorBox: {
        get value() { return stateRef.current.selectionAnchor; },
        set value(nextValue: number | null) { stateSettersRef.current.setSelectionAnchor(nextValue); },
      },
      currentFolderBox: {
        get value() { return stateRef.current.currentFolder; },
        set value(nextValue: FolderItem | null) {
          stateSettersRef.current.setCurrentFolder(nextValue);
          if (nextValue) { stateSettersRef.current.setCurrentFolderPath(nextValue.path); }
        },
      },
      itemsBox: {
        get value() { return stateRef.current.items; },
        set value(nextValue: HierarchyItem[]) { stateSettersRef.current.setItems(nextValue); },
      },
      loadingBox: {
        get value() { return stateRef.current.loading; },
        set value(nextValue: boolean) { stateSettersRef.current.setLoading(nextValue); },
      },
      loadingWithSkeletonBox: {
        get value() { return stateRef.current.loadingWithSkeleton; },
        set value(nextValue: boolean) { stateSettersRef.current.setLoadingWithSkeleton(nextValue); },
      },
      loadingNextPageBox: {
        get value() { return stateRef.current.loadingNextPage; },
        set value(nextValue: boolean) { stateSettersRef.current.setLoadingNextPage(nextValue); },
      },
      errorBox: {
        get value() { return stateRef.current.error; },
        set value(nextValue: string | null) { stateSettersRef.current.setError(nextValue); },
      },
      totalItemsBox: {
        get value() { return stateRef.current.totalItems; },
        set value(nextValue: number) { stateSettersRef.current.setTotalItems(nextValue); },
      },
      currentPageBox: {
        get value() { return stateRef.current.currentPage; },
        set value(nextValue: number) { stateSettersRef.current.setCurrentPage(nextValue); },
      },
      pageSizeBox: {
        get value() { return stateRef.current.pageSize; },
        set value(_nextValue: number) { /* Page size is static for React */ },
      },
      serverCapabilitiesBox: {
        get value() { return stateRef.current.serverCapabilities; },
        set value(nextValue: ServerCapabilities) { stateSettersRef.current.setServerCapabilities(nextValue); },
      },
      optionsInfoLoadingBox: {
        get value() { return stateRef.current.optionsInfoLoading; },
        set value(nextValue: boolean) { stateSettersRef.current.setOptionsInfoLoading(nextValue); },
      },
      sortAscendingBox: {
        get value() { return stateRef.current.sortAscending; },
        set value(nextValue: boolean) { stateSettersRef.current.setSortAscending(nextValue); },
      },
      sortColumnBox: {
        get value() { return stateRef.current.sortColumn; },
        set value(nextValue: string) { stateSettersRef.current.setSortColumn(nextValue); },
      },
      currentSearchQueryBox: {
        get value() { return stateRef.current.currentSearchQuery; },
        set value(nextValue: string | null) { stateSettersRef.current.setCurrentSearchQuery(nextValue); },
      },
      storedItemsBox: {
        get value() { return stateRef.current.storedItems; },
        set value(nextValue: HierarchyItem[]) { stateSettersRef.current.setStoredItems(nextValue); },
      },
      storedTypeBox: {
        get value() { return stateRef.current.storedType; },
        set value(nextValue: StoredType) { stateSettersRef.current.setStoredType(nextValue); },
      },
    };
  }

  const {
    selectedIndexesBox,
    selectionAnchorBox,
    currentFolderBox,
    itemsBox,
    loadingBox,
    loadingWithSkeletonBox,
    loadingNextPageBox,
    errorBox,
    totalItemsBox,
    currentPageBox,
    pageSizeBox,
    serverCapabilitiesBox,
    optionsInfoLoadingBox,
    sortAscendingBox,
    sortColumnBox,
    currentSearchQueryBox,
    storedItemsBox,
    storedTypeBox,
  } = boxesRef.current;

  const coreState = useMemo<FileBrowserCoreState>(
    () => ({
      currentFolder: currentFolderBox,
      items: itemsBox,
      loading: loadingBox,
      loadingWithSkeleton: loadingWithSkeletonBox,
      loadingNextPage: loadingNextPageBox,
      error: errorBox,
      totalItems: totalItemsBox,
      currentPage: currentPageBox,
      pageSize: pageSizeBox,
      sortAscending: sortAscendingBox,
      sortColumn: sortColumnBox,
      currentSearchQuery: currentSearchQueryBox,
      serverCapabilities: serverCapabilitiesBox,
      optionsInfoLoading: optionsInfoLoadingBox,
      selectedIndexes: selectedIndexesBox,
      selectionAnchor: selectionAnchorBox,
      storedItems: storedItemsBox,
      storedType: storedTypeBox,
    }),
    [] // eslint-disable-line react-hooks/exhaustive-deps -- boxes are stable refs, never recreated
  );

  const operations = useMemo(
    () =>
      createFileBrowserOperations({
        state: coreState,
        fileSystemRepository: repository,
        uiPorts,
        getSelectedItems: () => getSelectedItems(itemsBox.value, selectedIndexesBox.value),
        manageDocuments: (fileUrls, operation) => webDavClient.manageDocuments(fileUrls, operation),
        isDavProtocolSupported: () => webDavClient.isDavProtocolSupported(),
        downloadMultipleFiles,
      }),
    [] // eslint-disable-line react-hooks/exhaustive-deps -- all deps are stable: coreState and boxes are stable refs
  );

  const runSafely = useCallback(async (operation: () => Promise<void>) => {
    try {
      await operation();
    } catch {
      // Core state already contains the error message for UI display.
    }
  }, []);

  const loadFolder = useCallback(
    async (path: string) => {
      await runSafely(() => operations.loadFolder(path));
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const search = useCallback(
    async (query: string) => {
      await runSafely(() => operations.search(query));
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const clearSearch = useCallback(async () => {
    await runSafely(() => operations.clearSearch());
  }, []); // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable

  const refresh = useCallback(async () => {
    await runSafely(() => operations.refresh());
  }, []); // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable

  const setSort = useCallback((column: string, ascending: boolean) => {
    // Keep ref in sync immediately so route-orchestrated same-tick loads use new sort values.
    stateRef.current.sortColumn = column;
    stateRef.current.sortAscending = ascending;
    setSortColumn(column);
    setSortAscending(ascending);
  }, []);

  const toggleSort = useCallback(
    async (column: string) => {
      await runSafely(() => operations.changeSort(column));
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const nextPage = useCallback(
    async () => {
      await runSafely(() => operations.nextPage());
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const createFolderWithModal = useCallback(async () => {
    await runSafely(() => operations.createFolderWithModal());
  }, []); // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable

  const renameItemWithModal = useCallback(
    async (item?: HierarchyItem) => {
      await runSafely(() => operations.renameItemWithModal(item));
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const deleteItemsWithConfirmation = useCallback(
    async (itemsToDelete?: HierarchyItem[]) => {
      await runSafely(() => operations.deleteItemsWithConfirmation(itemsToDelete));
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const pasteStoredItems = useCallback(async () => {
    await runSafely(() => operations.pasteStoredItems());
  }, []); // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable

  const clearStoredItems = useCallback(() => {
    operations.clearStoredItems();
  }, []); // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable

  const updateItem = useCallback(
    async (path: string) => {
      await runSafely(() => operations.updateItem(path));
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const copyItemsToClipboard = useCallback(
    (itemsToCopy?: HierarchyItem[]) => {
      operations.copyItemsToClipboard(itemsToCopy);
    },
    [] // eslint-disable-line react-hooks/exhaustive-deps -- operations is stable
  );

  const cutItemsToClipboard = useCallback(
    (itemsToCut?: HierarchyItem[]) => {
      operations.cutItemsToClipboard(itemsToCut);
    },
    [operations]
  );

  const printItems = useCallback(() => {
    operations.printItems();
  }, [operations]);

  const editItem = useCallback(
    (item?: HierarchyItem) => {
      operations.editItem(item);
    },
    [operations]
  );

  const editItemWith = useCallback(
    (item?: HierarchyItem) => {
      operations.editItemWith(item);
    },
    [operations]
  );

  const openFolderInOsFileManager = useCallback(
    (folderPath: string) => {
      operations.openFolderInOsFileManager(folderPath);
    },
    [operations]
  );

  const lockFiles = useCallback(
    (itemsToLock?: HierarchyItem[]) => {
      operations.lockFiles(itemsToLock);
    },
    [operations]
  );

  const unlockFiles = useCallback(
    (itemsToUnlock?: HierarchyItem[]) => {
      operations.unlockFiles(itemsToUnlock);
    },
    [operations]
  );

  const previousPage = useCallback(
    async () => {
      await runSafely(() => operations.previousPage());
    },
    [operations, runSafely]
  );

  const toggleSelection = useCallback(
    (index: number) => {
      if (index < 0 || index >= items.length) {
        return;
      }

      operations.toggleSelection(index);
    },
    [items.length, operations]
  );

  const selectSingle = useCallback(
    (index: number) => {
      if (index < 0 || index >= items.length) {
        return;
      }

      operations.selectSingle(index);
    },
    [items.length, operations]
  );

  const setRangeFromAnchor = useCallback(
    (index: number) => {
      if (index < 0 || index >= items.length) {
        return;
      }

      operations.setRangeFromAnchor(index);
    },
    [items.length, operations]
  );

  const clearSelection = useCallback(() => {
    operations.clearSelection();
  }, [operations]);

  const selectAll = useCallback(() => {
    operations.selectAll();
  }, [operations]);

  const toggleSelectAll = useCallback(() => {
    operations.toggleSelectAll();
  }, [operations]);

  const moveSelection = useCallback(
    (delta: 1 | -1, extend = false) => {
      operations.moveSelection(delta, extend);
    },
    [operations]
  );

  const currentFolderPathRef = useRef(currentFolderPath);
  currentFolderPathRef.current = currentFolderPath;

  const searchSuggestions = useCallback(
    async (query: string): Promise<HierarchyItem[]> => {
      const folderPath = currentFolderPathRef.current;
      if (!folderPath || !query.trim()) {
        return [];
      }

      try {
        const result = await repository.searchInFolder(
          folderPath,
          query,
          PaginationOptions.default()
        );
        return result.items;
      } catch {
        return [];
      }
    },
    [repository]
  );

  const breadcrumbs = useMemo(() => buildBreadcrumbs(currentFolderPath), [currentFolderPath]);

  const selectedItems = useMemo(
    () => getSelectedItems(items, selectedIndexes),
    [items, selectedIndexes]
  );

  const countPages = useMemo(
    () => getCountPages(totalItems, pageSize),
    [pageSize, totalItems]
  );

  const hasNextPage = useMemo(
    () => hasNextPageState(currentPage, countPages),
    [countPages, currentPage]
  );

  const hasPreviousPage = useMemo(
    () => hasPreviousPageState(currentPage),
    [currentPage]
  );

  const isSearchMode = useMemo(
    () => isSearchModeState(currentSearchQuery),
    [currentSearchQuery]
  );

  const hasStoredItems = useMemo(() => storedItems.length > 0, [storedItems.length]);
  const isDavProtocolSupported = useMemo(() => operations.isDavProtocolSupported(), [operations]);
  const selectedFiles = useMemo(
    () => selectedItems.filter(isFileItem),
    [selectedItems]
  );
  const hasSingleFileSelection = selectedFiles.length === 1 && selectedItems.length === 1;
  const hasLockableSelection =
    serverCapabilities.supportsLocking &&
    selectedFiles.length > 0 &&
    selectedFiles.some(item => item.locks.length === 0);
  const hasUnlockableSelection =
    serverCapabilities.supportsLocking &&
    selectedFiles.length > 0 &&
    selectedFiles.some(item => item.locks.length > 0);

  const isAllSelected = useMemo(
    () => isAllSelectedState(items.length, selectedIndexes.length),
    [items.length, selectedIndexes.length]
  );

  const hasSelection = useMemo(
    () => hasSelectionState(selectedIndexes.length),
    [selectedIndexes.length]
  );

  return {
    title: 'File Browser',
    rootUrl,
    currentFolderPath,
    breadcrumbs,
    items,
    loading,
    loadingWithSkeleton,
    error,
    searchQuery: currentSearchQuery ?? '',
    sortColumn,
    sortAscending,
    currentPage,
    pageSize,
    totalItems,
    countPages,
    hasNextPage,
    hasPreviousPage,
    isSearchMode,
    loadingNextPage,
    serverCapabilities,
    optionsInfoLoading,
    storedItems,
    storedType,
    hasStoredItems,
    selectionAnchor,
    selectedIndexes,
    selectedItems,
    isAllSelected,
    hasSelection,
    loadFolder,
    search,
    clearSearch,
    refresh,
    setSort,
    toggleSort,
    nextPage,
    previousPage,
    createFolderWithModal,
    renameItemWithModal,
    deleteItemsWithConfirmation,
    copyItemsToClipboard,
    cutItemsToClipboard,
    pasteStoredItems,
    clearStoredItems,
    updateItem,
    printItems,
    editItem,
    editItemWith,
    openFolderInOsFileManager,
    lockFiles,
    unlockFiles,
    isDavProtocolSupported,
    hasSingleFileSelection,
    hasLockableSelection,
    hasUnlockableSelection,
    toggleSelection,
    selectSingle,
    setRangeFromAnchor,
    selectAll,
    clearSelection,
    toggleSelectAll,
    moveSelection,
    searchSuggestions,
  };
}
