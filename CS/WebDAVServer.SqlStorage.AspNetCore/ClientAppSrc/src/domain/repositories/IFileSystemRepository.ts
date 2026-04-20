import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { FolderItem } from '@/domain/entities/FolderItem';
import { PaginationOptions } from '@/domain/value-objects/PaginationOptions';
import { SortOptions } from '@/domain/value-objects/SortOptions';
import { ServerCapabilities } from '@/domain/value-objects/ServerCapabilities';

/**
 * Result of fetching folder contents
 */
export interface FolderContentResult {
  items: HierarchyItem[];
  totalItems: number;
  folder: FolderItem;
}

/**
 * Repository interface for file system operations
 * Abstracts the underlying WebDAV implementation
 */
export interface IFileSystemRepository {
  /**
   * Get the contents of a folder
   */
  getFolderContents(
    path: string,
    pagination: PaginationOptions,
    sort: SortOptions
  ): Promise<FolderContentResult>;

  /**
   * Search for items within a folder
   */
  searchInFolder(
    folderPath: string,
    searchQuery: string,
    pagination: PaginationOptions
  ): Promise<FolderContentResult>;

  /**
   * Get a single item by path
   */
  getItem(path: string): Promise<HierarchyItem>;

  /**
   * Create a new folder
   */
  createFolder(parentPath: string, folderName: string): Promise<FolderItem>;

  /**
   * Rename an item
   */
  renameItem(itemPath: string, newName: string): Promise<void>;

  /**
   * Delete items
   */
  deleteItems(itemPaths: string[]): Promise<void>;

  /**
   * Move items to a different folder
    * When overwrite is false (default) and an item already exists at the destination,
    * a CopyConflictError is thrown with the paths/names of the conflicting items so
    * the caller can prompt the user and retry with overwrite enabled.
   */
  moveItems(itemPaths: string[], targetPath: string, overwrite?: boolean): Promise<void>;

  /**
   * Copy items to a different folder.
   * When overwrite is false (default) and an item already exists at the destination
   * (and the source is in a different folder), a CopyConflictError is thrown with
   * the paths/names of the conflicting items so the caller can prompt the user.
   */
  copyItems(itemPaths: string[], targetPath: string, overwrite?: boolean): Promise<void>;

  /**
   * Check if a path exists
   */
  exists(path: string): Promise<boolean>;

  /**
   * Get supported features for a folder
   */
  getSupportedFeatures(folderPath: string): Promise<ServerCapabilities>;
}
