import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { FolderContentResult } from '@/domain/repositories/IFileSystemRepository';
import { PaginationOptions } from '@/domain/value-objects/PaginationOptions';

export interface SearchCoreResult {
  query: string;
  items: HierarchyItem[];
  totalItems: number;
}

export type SearchInFolderFn = (
  folderPath: string,
  query: string,
  pagination: PaginationOptions
) => Promise<FolderContentResult>;

export async function searchCore(
  folderPath: string,
  query: string,
  searchInFolder: SearchInFolderFn
): Promise<SearchCoreResult | null> {
  const normalizedQuery = query.trim();

  if (normalizedQuery.length === 0) {
    return null;
  }

  const pagination = PaginationOptions.default();
  const result = await searchInFolder(folderPath, normalizedQuery, pagination);

  return {
    query: normalizedQuery,
    items: result.items,
    totalItems: result.totalItems,
  };
}
