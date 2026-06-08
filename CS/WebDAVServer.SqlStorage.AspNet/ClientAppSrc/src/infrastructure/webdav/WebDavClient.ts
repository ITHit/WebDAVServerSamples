import { ITHit } from 'webdav.client';
import { WebDavSettings } from '@/infrastructure/config/webDavSettings';
import { getServerRootUrl } from '@/infrastructure/services/webDavBaseUrl';

export interface IUploaderCore {
  SetUploadUrl(url: string): void;
  Queue: {
    AddListener(event: string, handler: (event: never) => void, context?: unknown): void;
    RemoveListener(event: string, handler: (event: never) => void, context?: unknown): void;
  };
  Inputs: { AddById(id: string): void };
  DropZones: { AddById(id: string): void };
}

/**
 * Property name for search snippets
 */
export const snippetPropertyName = new ITHit.WebDAV.Client.PropertyName(
  'snippet',
  'ithit'
);

/**
 * Wrapper around WebDAV session
 * Provides promise-based interface to WebDAV operations
 */
export class WebDavClient {
  private session: ITHit.WebDAV.Client.WebDavSession;

  constructor() {
    this.session = new ITHit.WebDAV.Client.WebDavSession();
  }

  /**
   * Open a folder
   */
  async openFolder(path: string): Promise<ITHit.WebDAV.Client.Folder> {
    return new Promise((resolve, reject) => {
      this.session.OpenFolderAsync(path, [], (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve(result.Result as ITHit.WebDAV.Client.Folder);
      });
    });
  }

  /**
   * Open an item (file or folder)
   */
  async openItem(path: string): Promise<ITHit.WebDAV.Client.HierarchyItem> {
    return new Promise((resolve, reject) => {
      this.session.OpenItemAsync(path, [], (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve(result.Result);
      });
    });
  }

  /**
   * Get folder contents with pagination and sorting
   */
  async getFolderPage(
    folder: ITHit.WebDAV.Client.Folder,
    offset: number,
    pageSize: number,
    sortColumns: ITHit.WebDAV.Client.OrderProperty[]
  ): Promise<{ items: ITHit.WebDAV.Client.HierarchyItem[]; totalItems: number }> {
    return new Promise((resolve, reject) => {
      folder.GetPageAsync([], offset, pageSize, sortColumns, (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve({
          items: result.Result.Page,
          totalItems: result.Result.TotalItems
        });
      });
    });
  }

  /**
   * Search within a folder
   */
  async searchInFolder(
    folder: ITHit.WebDAV.Client.Folder,
    searchQuery: string,
    offset: number,
    pageSize: number
  ): Promise<{ items: ITHit.WebDAV.Client.HierarchyItem[]; totalItems: number }> {
    const query = new ITHit.WebDAV.Client.SearchQuery(
      searchQuery
        .replace(/\\/g, '\\\\')
        .replace(/\\%/g, '\\%')
        .replace(/\\_/g, '\\_')
        .replace(/\*/g, '%')
        .replace(/\?/g, '_') + '%'
    );

    query.SelectProperties = [
      new ITHit.WebDAV.Client.PropertyName('snippet', 'ithit')
    ];

    return new Promise((resolve, reject) => {
      folder.GetSearchPageByQueryAsync(query, offset, pageSize, (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve({
          items: result.Result.Page,
          totalItems: result.Result.TotalItems
        });
      });
    });
  }

  /**
   * Create a folder
   */
  async createFolder(
    parentFolder: ITHit.WebDAV.Client.Folder,
    folderName: string
  ): Promise<ITHit.WebDAV.Client.Folder> {
    return new Promise((resolve, reject) => {
      parentFolder.CreateFolderAsync(folderName, '', [], (result) => {
        if (!result.IsSuccess) {
          return reject(result);
        }
        resolve(result.Result as ITHit.WebDAV.Client.Folder);
      });
    });
  }

  /**
   * Get supported features for a folder
   */
  async getSupportedFeatures(
    folder: ITHit.WebDAV.Client.Folder
  ): Promise<ITHit.WebDAV.Client.OptionsInfo> {
    return new Promise((resolve, reject) => {
      folder.GetSupportedFeaturesAsync((result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve(result.Result);
      });
    });
  }

  /**
   * Create sort columns array
   */
  createSortColumns(
    sortColumn: string,
    sortAscending: boolean
  ): ITHit.WebDAV.Client.OrderProperty[] {
    const namespaceUri = 'DAV:';
    const sortColumns = [
      new ITHit.WebDAV.Client.OrderProperty(
        new ITHit.WebDAV.Client.PropertyName('is-directory', namespaceUri),
        false
      )
    ];

    if (sortColumn) {
      sortColumns.push(
        new ITHit.WebDAV.Client.OrderProperty(
          new ITHit.WebDAV.Client.PropertyName(sortColumn, namespaceUri),
          sortAscending
        )
      );
    }

    return sortColumns;
  }

  /**
   * Delete an item (file or folder)
   */
  async deleteItem(item: ITHit.WebDAV.Client.HierarchyItem): Promise<void> {
    return new Promise((resolve, reject) => {
      item.DeleteAsync(null, (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve();
      });
    });
  }

  /**
   * Rename/move an item
   */
  async moveItem(
    item: ITHit.WebDAV.Client.HierarchyItem,
    targetFolder: ITHit.WebDAV.Client.Folder,
    newName: string,
    overwrite: boolean = false
  ): Promise<void> {
    return new Promise((resolve, reject) => {
      item.MoveToAsync(targetFolder, newName, overwrite, [], (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve();
      });
    });
  }

  /**
   * Copy an item
   */
  async copyItem(
    item: ITHit.WebDAV.Client.HierarchyItem,
    targetFolder: ITHit.WebDAV.Client.Folder,
    newName: string,
    deep: boolean = true,
    overwrite: boolean = false
  ): Promise<void> {
    return new Promise((resolve, reject) => {
      item.CopyToAsync(targetFolder, newName, deep, overwrite, [], (result) => {
        if (!result.IsSuccess) {
          return reject(result.Error);
        }
        resolve();
      });
    });
  }

  /**
   * Open an item (callback-based for legacy code)
   */
  openItemCallback(
    path: string,
    callback: (result: ITHit.WebDAV.Client.AsyncResult) => void
  ): void {
    this.session.OpenItemAsync(path, [], callback);
  }

  static getWebdavClientVersion(): string {
    return ITHit.WebDAV.Client.WebDavSession.Version;
  }

  /**
   * Check if an item is a folder
   */
  static isFolder(item: ITHit.WebDAV.Client.HierarchyItem): boolean {
    return item.ResourceType === ITHit.WebDAV.Client.ResourceType.Folder;
  }

  /**
   * Check if an item is a file
   */
  static isFile(item: ITHit.WebDAV.Client.HierarchyItem): boolean {
    return item instanceof ITHit.WebDAV.Client.File;
  }

  /**
   * Get file from hierarchy item (with type safety)
   */
  static asFile(item: ITHit.WebDAV.Client.HierarchyItem): ITHit.WebDAV.Client.File | null {
    return WebDavClient.isFile(item) ? (item as ITHit.WebDAV.Client.File) : null;
  }

  isDavProtocolSupported(): boolean {
    return ITHit.WebDAV.Client.DocManager.IsDavProtocolSupported();
  }

  isMicrosoftOfficeDocument(href: string): boolean {
    return ITHit.WebDAV.Client.DocManager.IsMicrosoftOfficeDocument(href);
  }

  manageDocuments(
    fileUrls: string[],
    operation: 'Edit' | 'Print' | 'Lock' | 'Unlock' | 'OpenWith',
    showProtocolInstallModal: (...args: unknown[]) => void,
  ): void {
    ITHit.WebDAV.Client.DocManager.DavProtocolEditDocument(
      fileUrls,
      getServerRootUrl(),
      showProtocolInstallModal,
      null,
      WebDavSettings.EditDocAuth.SearchIn,
      WebDavSettings.EditDocAuth.CookieNames,
      WebDavSettings.EditDocAuth.LoginUrl,
      operation != 'Edit' ? operation : undefined
    );
  }

  createUploaderCore(): IUploaderCore {
    return new ITHit.WebDAV.Client.Upload.Uploader() as unknown as IUploaderCore;
  }

  static getInstallerFileUrl(): string {
    const agent = navigator.userAgent;
    if (agent.indexOf('Win') > -1) {
      return 'ms-windows-store://pdp/?ProductId=9nqb82r5hmnh';
    } else if (agent.indexOf('Mac') > -1) {
      return 'https://apps.apple.com/us/app/webdav-drive/id6502366145';
    }
    return (
      WebDavSettings.ApplicationProtocolsPath +
      ITHit.WebDAV.Client.DocManager.GetProtocolInstallFileNames()[0]
    );
  }

  /**
   * Open a folder in the OS file manager via the IT Hit protocol handler
   */
  static openFolderInOsFileManager(
    folderHref: string,
    serverRootUrl: string,
    showProtocolInstallModal: (...args: unknown[]) => void,
    mountUrl: string | null,
    searchIn: string,
    cookieNames: string,
    loginUrl: string
  ): void {
    ITHit.WebDAV.Client.DocManager.OpenFolderInOsFileManager(
      folderHref,
      serverRootUrl,
      showProtocolInstallModal,
      mountUrl,
      searchIn,
      cookieNames,
      loginUrl
    );
  }
}
