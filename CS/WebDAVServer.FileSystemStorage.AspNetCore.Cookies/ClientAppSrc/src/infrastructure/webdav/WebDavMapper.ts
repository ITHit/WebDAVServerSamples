import { ITHit } from 'webdav.client';
import { HierarchyItem } from '@/domain/entities/HierarchyItem';
import { FileItem } from '@/domain/entities/FileItem';
import { FolderItem } from '@/domain/entities/FolderItem';
import { ServerCapabilities } from '@/domain/value-objects/ServerCapabilities';

/**
 * Maps WebDAV client objects to domain entities.
 *
 * Also maintains a URL-keyed cache of the original ITHit objects so that
 * subsequent operations (e.g. rename) can skip redundant PROPFIND requests
 * when the object was already fetched as part of a folder listing.
 */
export class WebDavMapper {
  /**
   * Maximum number of ITHit objects kept in the LRU cache.
   * A typical folder page is ≤ 100 items; 1000 entries covers several pages
   * without risking unbounded memory growth.
   */
  static readonly CACHE_MAX_SIZE = 1000;

  /**
   * LRU cache: absolute Href → original ITHit HierarchyItem (or Folder).
   * Map insertion order is used to track recency — the first entry is always
   * the least recently used and is evicted first when the limit is reached.
   * Populated whenever an ITHit object is mapped to a domain entity.
   * Invalidated explicitly after mutating operations.
   */
  private static readonly _cache = new Map<string, ITHit.WebDAV.Client.HierarchyItem>();

  /** Insert or refresh an entry, evicting the LRU entry if the cache is full. */
  private static _cacheSet(href: string, item: ITHit.WebDAV.Client.HierarchyItem): void {
    // Re-insert to move to the end (most recently used)
    WebDavMapper._cache.delete(href);
    WebDavMapper._cache.set(href, item);
    // Evict oldest entry when over the limit
    if (WebDavMapper._cache.size > WebDavMapper.CACHE_MAX_SIZE) {
      const oldest = WebDavMapper._cache.keys().next().value;
      if (oldest !== undefined) {
        WebDavMapper._cache.delete(oldest);
      }
    }
  }

  /** Return the cached raw ITHit item for the given absolute URL, if any. */
  static getCachedItem(href: string): ITHit.WebDAV.Client.HierarchyItem | undefined {
    const item = WebDavMapper._cache.get(href);
    if (item !== undefined) {
      // Refresh recency
      WebDavMapper._cache.delete(href);
      WebDavMapper._cache.set(href, item);
    }
    return item;
  }

  /** Return the cached raw ITHit Folder for the given absolute URL, if any. */
  static getCachedFolder(href: string): ITHit.WebDAV.Client.Folder | undefined {
    const item = WebDavMapper.getCachedItem(href);
    if (item && item.ResourceType === ITHit.WebDAV.Client.ResourceType.Folder) {
      return item as ITHit.WebDAV.Client.Folder;
    }
    return undefined;
  }

  /**
   * Remove one or more entries from the cache.
   * Call after any mutation that changes an item's URL (rename, move, delete).
   * Pass no arguments to clear the entire cache.
   */
  static invalidate(...hrefs: string[]): void {
    if (hrefs.length === 0) {
      WebDavMapper._cache.clear();
    } else {
      for (const href of hrefs) {
        WebDavMapper._cache.delete(href);
      }
    }
  }

  /**
   * Map ITHit HierarchyItem to domain HierarchyItem.
   * The original ITHit object is stored in the cache keyed by its Href.
   */
  static toDomainHierarchyItem(
    item: ITHit.WebDAV.Client.HierarchyItem
  ): HierarchyItem {
    WebDavMapper._cacheSet(item.Href, item);

    const isFolder = item.ResourceType === ITHit.WebDAV.Client.ResourceType.Folder;
    const locks = item.ActiveLocks?.map(lock => lock.LockToken.LockToken) || [];

    if (isFolder) {
      return new FolderItem(
        item.Href,
        item.DisplayName,
        item.Href,
        item.LastModified,
        locks
      );
    } else {
      const file = item as ITHit.WebDAV.Client.File;
      return new FileItem(
        item.Href,
        item.DisplayName,
        item.Href,
        file.ContentLength || 0,
        item.LastModified,
        file.ContentType || '',
        locks
      );
    }
  }

  /**
   * Map array of ITHit items to domain items.
   * Each item is also stored in the cache.
   */
  static toDomainHierarchyItems(
    items: ITHit.WebDAV.Client.HierarchyItem[]
  ): HierarchyItem[] {
    return items.map(item => this.toDomainHierarchyItem(item));
  }

  /**
   * Map ITHit Folder to domain FolderItem.
   * The original ITHit Folder object is stored in the cache keyed by its Href.
   */
  static toDomainFolder(folder: ITHit.WebDAV.Client.Folder): FolderItem {
    WebDavMapper._cacheSet(folder.Href, folder);

    const locks = folder.ActiveLocks?.map(lock => lock.LockToken.LockToken) || [];
    return new FolderItem(
      folder.Href,
      folder.DisplayName,
      folder.Href,
      folder.LastModified,
      locks
    );
  }

  /**
   * Map ITHit OptionsInfo to domain ServerCapabilities
   */
  static toServerCapabilities(optionsInfo: ITHit.WebDAV.Client.OptionsInfo): ServerCapabilities {
    return new ServerCapabilities(
      !!(optionsInfo.Features & ITHit.WebDAV.Client.Features.Dasl), // supportsSearch
      true, // supportsLocking - assume supported
      !!(optionsInfo.Features & ITHit.WebDAV.Client.Features.VersionControl), // supportsVersioning
      !!(optionsInfo.Features & ITHit.WebDAV.Client.Features.ResumableUpload) // supportsResumableUpload
    );
  }
}
