import { useEffect, useMemo, useRef } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import type { FileBrowserViewModel } from '@/features/hooks/useFileBrowser';
import { createGridRouteOrchestrator } from '@/features/models/gridRouteOrchestrator';
import { bindGridRouteEventBusHandlers } from '@/features/models/gridRouteEventBusBindings';
import { bindGridRouteSignals } from '@/features/models/gridRouteSignalBindings';
import type { GridRouteOrchestrationDeps } from '@/features/models/gridRouteOrchestrationDeps';
import type { MutableBox } from '@/shared/types/box';

type RouteQuery = Record<string, unknown>;

function getSortFromQuery(searchParams: URLSearchParams): { column: string; ascending: boolean } {
  const sortcolumn = searchParams.get('sortcolumn');
  const sortascending = searchParams.get('sortascending');
  return {
    column: sortcolumn && sortcolumn.length > 0 ? sortcolumn : 'displayname',
    ascending: sortascending !== 'false',
  };
}

function getQueryFromLocation(search: string): RouteQuery {
  const params = new URLSearchParams(search);
  const query: RouteQuery = {};
  params.forEach((value, key) => {
    query[key] = value;
  });
  return query;
}

function getSearchQueryValue(search: string): string | undefined {
  const value = new URLSearchParams(search).get('search');
  return value && value.length > 0 ? value : undefined;
}

function queryToSearch(query: RouteQuery): string {
  const params = new URLSearchParams();
  Object.entries(query).forEach(([key, value]) => {
    if (typeof value === 'string') {
      params.set(key, value);
    }
  });
  const search = params.toString();
  return search.length > 0 ? `?${search}` : '';
}

export function useGridRouteOrchestration(fileBrowser: FileBrowserViewModel, deps: GridRouteOrchestrationDeps) {
  const location = useLocation();
  const navigate = useNavigate();

  const locationRef = useRef(location);
  locationRef.current = location;

  const fileBrowserRef = useRef(fileBrowser);
  fileBrowserRef.current = fileBrowser;

  const sortStateRef = useRef({
    column: fileBrowser.sortColumn,
    ascending: fileBrowser.sortAscending,
  });

  useEffect(() => {
    sortStateRef.current.column = fileBrowser.sortColumn;
    sortStateRef.current.ascending = fileBrowser.sortAscending;
  }, [fileBrowser.sortAscending, fileBrowser.sortColumn]);

  const pathAndSearchHandlersRef = useRef(new Set<(newPath: string, oldPath: string | undefined) => void | Promise<void>>());
  const sortHandlersRef = useRef(new Set<() => void>());
  const folderPathHandlersRef = useRef(new Set<(folderPath: string | undefined) => void | Promise<void>>());

  const sortColumnBox = useMemo<MutableBox<string>>(
    () => ({
      get value() {
        return fileBrowserRef.current.sortColumn;
      },
      set value(nextValue: string) {
        sortStateRef.current.column = nextValue;
        fileBrowserRef.current.setSort(nextValue, sortStateRef.current.ascending);
      },
    }),
    []
  );

  const sortAscendingBox = useMemo<MutableBox<boolean>>(
    () => ({
      get value() {
        return fileBrowserRef.current.sortAscending;
      },
      set value(nextValue: boolean) {
        sortStateRef.current.ascending = nextValue;
        fileBrowserRef.current.setSort(sortStateRef.current.column, nextValue);
      },
    }),
    []
  );

  const currentFolderBox = useMemo<MutableBox<{ path: string } | null>>(
    () => ({
      get value() {
        const currentPath = fileBrowserRef.current.currentFolderPath;
        return currentPath ? { path: currentPath } : null;
      },
      set value(_nextValue: { path: string } | null) {
        // Route orchestrator never writes to this box directly.
      },
    }),
    []
  );

  const orchestrator = useMemo(
    () =>
      createGridRouteOrchestrator(
        {
          sortColumn: sortColumnBox,
          sortAscending: sortAscendingBox,
          currentFolder: currentFolderBox,
          loadFolder: (path: string, showSkeleton?: boolean) =>
            fileBrowserRef.current.loadFolder(path, showSkeleton),
          search: (query: string) => fileBrowserRef.current.search(query),
          clearSearch: () => fileBrowserRef.current.clearSearch(),
          refresh: () => fileBrowserRef.current.refresh(),
        },
        deps,
        deps.reportError
      ),
    [currentFolderBox, deps, sortAscendingBox, sortColumnBox]
  );

  useEffect(() => {
    void orchestrator.onInitialLoad(locationRef.current.pathname, getQueryFromLocation(locationRef.current.search));

    const unbindEventBusHandlers = bindGridRouteEventBusHandlers({
      eventBus: deps.eventBus,
      onFolderRefreshRequested: () => {
        void fileBrowserRef.current.refresh();
      },
      onItemUpdated: (path: string) => {
        void fileBrowserRef.current.updateItem(path).catch(deps.reportError);
      },
      onErrorOccurred: deps.reportEventBusError,
    });

    const unbindRouteSignals = bindGridRouteSignals(
      {
        subscribePathAndSearchChanged: handler => {
          pathAndSearchHandlersRef.current.add(handler);
          return () => {
            pathAndSearchHandlersRef.current.delete(handler);
          };
        },
        subscribeSortChanged: handler => {
          sortHandlersRef.current.add(handler);
          return () => {
            sortHandlersRef.current.delete(handler);
          };
        },
        subscribeFolderPathChanged: handler => {
          folderPathHandlersRef.current.add(handler);
          return () => {
            folderPathHandlersRef.current.delete(handler);
          };
        },
      },
      {
        onPathAndSearchChanged: (newPath, oldPath) =>
          orchestrator.onPathAndSearchChanged(
            newPath,
            oldPath,
            getQueryFromLocation(locationRef.current.search)
          ),
        onSortChanged: () => {
          orchestrator.onSortChanged(getQueryFromLocation(locationRef.current.search));
        },
        onFolderPathChanged: folderPath =>
          orchestrator.onFolderPathChanged(
            folderPath,
            locationRef.current.pathname,
            getQueryFromLocation(locationRef.current.search),
            async (path, query) => {
              navigate({ pathname: path, search: queryToSearch(query) });
              return Promise.resolve();
            }
          ),
      }
    );

    return () => {
      unbindEventBusHandlers();
      unbindRouteSignals();
    };
  }, [deps, navigate, orchestrator]);

  const previousPathRef = useRef<string | undefined>(location.pathname);
  const previousSearchQueryRef = useRef<string | undefined>(
    getSearchQueryValue(location.search)
  );
  const previousSearchRef = useRef<string>(location.search);

  useEffect(() => {
    const oldPath = previousPathRef.current;
    const oldSearchQuery = previousSearchQueryRef.current;
    const newSearchQuery = getSearchQueryValue(location.search);

    previousPathRef.current = location.pathname;
    previousSearchQueryRef.current = newSearchQuery;
    locationRef.current = location;

    if (oldPath === location.pathname && oldSearchQuery === newSearchQuery) {
      return;
    }

    pathAndSearchHandlersRef.current.forEach(handler => {
      void handler(location.pathname, oldPath);
    });
  }, [location, location.pathname, location.search]);

  useEffect(() => {
    const prevParams = new URLSearchParams(previousSearchRef.current);
    const currentParams = new URLSearchParams(location.search);
    const prevSort = getSortFromQuery(prevParams);
    const currSort = getSortFromQuery(currentParams);

    if (prevSort.column !== currSort.column || prevSort.ascending !== currSort.ascending) {
      sortHandlersRef.current.forEach(handler => {
        handler();
      });
    }

    previousSearchRef.current = location.search;
  }, [location.search]);

  const previousFolderPathRef = useRef<string | undefined>(fileBrowser.currentFolderPath);

  useEffect(() => {
    const folderPath = fileBrowser.currentFolderPath || undefined;
    if (previousFolderPathRef.current === folderPath) {
      return;
    }

    previousFolderPathRef.current = folderPath;
    folderPathHandlersRef.current.forEach(handler => {
      void handler(folderPath);
    });
  }, [fileBrowser.currentFolderPath]);

  useEffect(() => {
    if (fileBrowser.error) {
      deps.logLocalizedError(fileBrowser.error);
    }
  }, [deps, fileBrowser.error]);
}
