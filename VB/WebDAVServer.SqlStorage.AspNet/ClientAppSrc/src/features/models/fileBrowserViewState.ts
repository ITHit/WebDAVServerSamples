export function getCountPages(totalItems: number, pageSize: number): number {
  return Math.ceil(totalItems / pageSize);
}

export function hasNextPage(currentPage: number, countPages: number): boolean {
  return currentPage < countPages;
}

export function hasPreviousPage(currentPage: number): boolean {
  return currentPage > 1;
}

export function hasStoredItems(storedItemsCount: number): boolean {
  return storedItemsCount > 0;
}

export function isLoading(loading: boolean, loadingWithSkeleton: boolean): boolean {
  return loading || loadingWithSkeleton;
}

export function isSearchMode(currentSearchQuery: string | null): boolean {
  return currentSearchQuery !== null;
}
