import {
  IFileSystemRepository,
  FolderContentResult
} from '@/domain/repositories/IFileSystemRepository';
import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { FolderItem } from '@/domain/entities/FolderItem';
import { PaginationOptions } from '@/domain/value-objects/PaginationOptions';
import { SortOptions } from '@/domain/value-objects/SortOptions';
import { ServerCapabilities } from '@/domain/value-objects/ServerCapabilities';
import { ITHit } from 'webdav.client';
import { WebDavClient } from '@/infrastructure/webdav/WebDavClient';
import { WebDavMapper } from '@/infrastructure/webdav/WebDavMapper';
import { FormatUtils } from '@/shared/utils/formatUtils';
import { CopyConflictError } from '@/shared/types/appErrors';
/**
 * WebDAV implementation of the file system repository
 */
export class WebDavFileSystemRepository implements IFileSystemRepository {
  constructor(private webDavClient: WebDavClient) { }

  async getFolderContents(
    path: string,
    pagination: PaginationOptions,
    sort: SortOptions
  ): Promise<FolderContentResult> {
    // Ensure path ends with /
    const normalizedPath = path.replace(/\/?$/, '/');

    // Open the folder
    const folder = await this.webDavClient.openFolder(normalizedPath);

    // Get folder contents
    const sortColumns = this.webDavClient.createSortColumns(
      sort.column,
      sort.ascending
    );

    const result = await this.webDavClient.getFolderPage(
      folder,
      pagination.offset,
      pagination.pageSize,
      sortColumns
    );

    return {
      folder: WebDavMapper.toDomainFolder(folder),
      items: WebDavMapper.toDomainHierarchyItems(result.items),
      totalItems: result.totalItems
    };
  }

  async searchInFolder(
    folderPath: string,
    searchQuery: string,
    pagination: PaginationOptions
  ): Promise<FolderContentResult> {
    const normalizedPath = folderPath.replace(/\/?$/, '/');
    const folder = await this.resolveFolder(normalizedPath);

    const result = await this.webDavClient.searchInFolder(
      folder,
      searchQuery,
      pagination.offset,
      pagination.pageSize
    );

    return {
      folder: WebDavMapper.toDomainFolder(folder),
      items: WebDavMapper.toDomainHierarchyItems(result.items),
      totalItems: result.totalItems
    };
  }

  async getItem(path: string): Promise<HierarchyItem> {
    const item = await this.webDavClient.openItem(path);
    return WebDavMapper.toDomainHierarchyItem(item);
  }

  async createFolder(parentPath: string, folderName: string): Promise<FolderItem> {
    const normalizedPath = parentPath.replace(/\/?$/, '/');
    const parentFolder = await this.webDavClient.openFolder(normalizedPath);
    const newFolder = await this.webDavClient.createFolder(parentFolder, folderName);
    return WebDavMapper.toDomainFolder(newFolder);
  }

  async renameItem(itemPath: string, newName: string): Promise<void> {
    // Derive parent path (pure string — no network)
    const normalizedItemPath = itemPath.replace(/\/+$/, '');
    const lastSlash = normalizedItemPath.lastIndexOf('/');
    const parentPath = lastSlash > 0
      ? `${normalizedItemPath.substring(0, lastSlash)}/`
      : '/';

    // Use cached ITHit objects when available (avoids up to 2 PROPFIND requests)
    const item = await this.resolveItem(itemPath);
    const parentFolder = await this.resolveFolder(parentPath);

    // Rename is a move to the same folder with a new name
    await this.webDavClient.moveItem(item, parentFolder, newName);

    // The old URL no longer exists — remove from cache
    WebDavMapper.invalidate(itemPath);
  }

  async deleteItems(itemPaths: string[]): Promise<void> {
    // Delete all items in parallel
    await Promise.all(
      itemPaths.map(async (path) => {
        const item = await this.resolveItem(path);
        await this.webDavClient.deleteItem(item);
      })
    );
    // Items no longer exist — purge from cache
    WebDavMapper.invalidate(...itemPaths);
  }

  async moveItems(itemPaths: string[], targetPath: string, overwrite: boolean = false): Promise<void> {
    const normalizedTargetPath = targetPath.replace(/\/?$/, '/');
    const targetFolder = await this.resolveFolder(normalizedTargetPath);
    const conflictingPaths: string[] = [];
    const conflictingNames: string[] = [];

    await Promise.all(
      itemPaths.map(async (path) => {
        const item = await this.resolveItem(path);
        const normalizedPath = path.replace(/\/?$/, '');
        const itemName = normalizedPath.substring(normalizedPath.lastIndexOf('/') + 1);

        try {
          await this.webDavClient.moveItem(item, targetFolder, itemName, overwrite);
          WebDavMapper.invalidate(path);
        } catch (error) {
          if (
            !overwrite && (
              error instanceof ITHit.WebDAV.Client.Exceptions.PreconditionFailedException ||
              error instanceof ITHit.WebDAV.Client.Exceptions.ForbiddenException
            )
          ) {
            conflictingPaths.push(path);
            conflictingNames.push(itemName);
            return;
          }

          throw error;
        }
      })
    );

    if (conflictingPaths.length > 0) {
      throw new CopyConflictError(conflictingPaths, conflictingNames);
    }
  }

  async copyItems(itemPaths: string[], targetPath: string, overwrite: boolean = false): Promise<void> {
    const normalizedTargetPath = targetPath.replace(/\/?$/, '/');
    const targetFolder = await this.resolveFolder(normalizedTargetPath);

    const conflictingPaths: string[] = [];
    const conflictingNames: string[] = [];

    await Promise.all(
      itemPaths.map(async (path) => {
        const item = await this.resolveItem(path);
        const normalizedPath = path.replace(/\/?$/, '');
        const baseName = normalizedPath.substring(normalizedPath.lastIndexOf('/') + 1);
        const sourceFolder = normalizedPath.substring(0, normalizedPath.lastIndexOf('/') + 1);
        const isSameFolder = sourceFolder === normalizedTargetPath;

        if (isSameFolder) {
          // Same folder → auto-rename with "- Copy (n)" suffix loop
          const MAX_COPY_ATTEMPTS = 50;
          let copyName = baseName;
          for (let attempt = 0; attempt < MAX_COPY_ATTEMPTS; attempt++) {
            try {
              await this.webDavClient.copyItem(item, targetFolder, copyName, true, false);
              break;
            } catch (error) {
              if (
                error instanceof ITHit.WebDAV.Client.Exceptions.PreconditionFailedException ||
                error instanceof ITHit.WebDAV.Client.Exceptions.ForbiddenException
              ) {
                if (attempt === MAX_COPY_ATTEMPTS - 1) throw error;
                copyName = FormatUtils.getNameWithCopySuffix(copyName, true);
              } else {
                throw error;
              }
            }
          }
        } else if (overwrite) {
          // Different folder + user confirmed overwrite
          await this.webDavClient.copyItem(item, targetFolder, baseName, true, true);
        } else {
          // Different folder, first attempt: try without overwrite, collect conflicts
          try {
            await this.webDavClient.copyItem(item, targetFolder, baseName, true, false);
          } catch (error) {
            if (
              error instanceof ITHit.WebDAV.Client.Exceptions.PreconditionFailedException ||
              error instanceof ITHit.WebDAV.Client.Exceptions.ForbiddenException
            ) {
              conflictingPaths.push(path);
              conflictingNames.push(baseName);
            } else {
              throw error;
            }
          }
        }
      })
    );

    if (conflictingPaths.length > 0) {
      throw new CopyConflictError(conflictingPaths, conflictingNames);
    }
  }

  async exists(path: string): Promise<boolean> {
    try {
      await this.webDavClient.openItem(path);
      return true;
    } catch {
      return false;
    }
  }

  async getSupportedFeatures(folderPath: string): Promise<ServerCapabilities> {
    const normalizedPath = folderPath.replace(/\/?$/, '/');
    const folder = await this.webDavClient.openFolder(normalizedPath);
    const optionsInfo = await this.webDavClient.getSupportedFeatures(folder);
    return WebDavMapper.toServerCapabilities(optionsInfo);
  }


  /** Returns the cached ITHit item, fetching it via PROPFIND only on a cache miss. */
  private async resolveItem(path: string): Promise<ITHit.WebDAV.Client.HierarchyItem> {
    return WebDavMapper.getCachedItem(path) ?? await this.webDavClient.openItem(path);
  }

  /** Returns the cached ITHit folder, fetching it via PROPFIND only on a cache miss. */
  private async resolveFolder(path: string): Promise<ITHit.WebDAV.Client.Folder> {
    return WebDavMapper.getCachedFolder(path) ?? await this.webDavClient.openFolder(path);
  }
}
