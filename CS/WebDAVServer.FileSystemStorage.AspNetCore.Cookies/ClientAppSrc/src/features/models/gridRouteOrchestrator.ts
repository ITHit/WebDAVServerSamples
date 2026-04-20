import { getAppPathFromServerUrl } from '@/infrastructure/services/webDavBaseUrl';
import type { MutableBox } from '@/shared/types/box';

type RouteQuery = Record<string, unknown>;

interface GridFileBrowserRouteApi {
  sortColumn: MutableBox<string>;
  sortAscending: MutableBox<boolean>;
  currentFolder: MutableBox<{ path: string } | null>;
  loadFolder(path: string, showSkeleton?: boolean): Promise<void>;
  search(query: string): Promise<void>;
  clearSearch(): Promise<void>;
  refresh(preservePagination?: boolean, forceRefresh?: boolean): Promise<void>;
}

export interface GridRouteOrchestrationDeps {
  getServerRootUrl: () => string;
  getServerUrl: (path: string) => string;
}

function getSearchQuery(query: RouteQuery): string | undefined {
  return typeof query.search === 'string' && query.search ? query.search : undefined;
}

function applySortFromQuery(fileBrowser: GridFileBrowserRouteApi, query: RouteQuery) {
  const sortcolumn = query.sortcolumn;
  const sortascending = query.sortascending;
  fileBrowser.sortColumn.value = typeof sortcolumn === 'string' && sortcolumn ? sortcolumn : 'displayname';
  fileBrowser.sortAscending.value = sortascending !== 'false';
}

function getQueryWithoutSearch(query: RouteQuery): RouteQuery {
  return Object.fromEntries(Object.entries(query).filter(([k]) => k !== 'search'));
}

function toAppPath(folderPath: string): string {
  return getAppPathFromServerUrl(folderPath);
}

export function createGridRouteOrchestrator(
  fileBrowser: GridFileBrowserRouteApi,
  deps: GridRouteOrchestrationDeps,
  handleError: (error: unknown) => void
) {
  let syncingFromRoute = false;
  let syncingFromFolder = false;

  const navigateToPath = async (path: string, query: RouteQuery, showSkeleton = false) => {
    applySortFromQuery(fileBrowser, query);
    try {
      syncingFromRoute = true;
      await fileBrowser.loadFolder(deps.getServerUrl(path), showSkeleton);
    } catch (err) {
      handleError(err);
    } finally {
      syncingFromRoute = false;
    }
  };

  const onInitialLoad = async (path: string, query: RouteQuery) => {
    await navigateToPath(path, query, true);
    const searchQuery = getSearchQuery(query);
    if (searchQuery) {
      await fileBrowser.search(searchQuery).catch(handleError);
    }
  };

  const onPathAndSearchChanged = async (
    newPath: string,
    oldPath: string | undefined,
    query: RouteQuery
  ) => {
    if (syncingFromFolder) return;

    const pathChanged = newPath !== oldPath;
    const searchQuery = getSearchQuery(query);

    if (pathChanged) {
      if (fileBrowser.currentFolder.value?.path !== deps.getServerUrl(newPath)) {
        await navigateToPath(newPath, query, true);
      }
      if (searchQuery) {
        await fileBrowser.search(searchQuery).catch(handleError);
      }
    } else if (searchQuery) {
      await fileBrowser.search(searchQuery).catch(handleError);
    } else {
      await fileBrowser.clearSearch().catch(handleError);
    }
  };

  const onSortChanged = (query: RouteQuery) => {
    if (syncingFromRoute || syncingFromFolder) return;
    applySortFromQuery(fileBrowser, query);
    fileBrowser.refresh().catch(handleError);
  };

  const onFolderPathChanged = async (
    folderPath: string | undefined,
    routePath: string,
    routeQuery: RouteQuery,
    pushRoute: (path: string, query: RouteQuery) => Promise<unknown>
  ) => {
    if (!folderPath || syncingFromRoute) return;
    const appPath = toAppPath(folderPath);
    if (routePath === appPath) return;

    try {
      syncingFromFolder = true;
      await pushRoute(appPath, getQueryWithoutSearch(routeQuery));
    } finally {
      syncingFromFolder = false;
    }
  };

  return {
    onInitialLoad,
    onPathAndSearchChanged,
    onSortChanged,
    onFolderPathChanged,
  };
}
