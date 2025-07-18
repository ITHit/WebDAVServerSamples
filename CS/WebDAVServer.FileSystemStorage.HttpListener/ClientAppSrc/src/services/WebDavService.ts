import { ITHit } from "webdav.client";
import { UrlResolveService } from "./UrlResolveService";
import { WebDavError } from "../models/WebDavError";

const webDavSession = new ITHit.WebDAV.Client.WebDavSession();

export const snippetPropertyName = new ITHit.WebDAV.Client.PropertyName(
  "snippet",
  "ithit"
);

export interface CurrentFolderResult {
  page: ITHit.WebDAV.Client.HierarchyItem[];
  totalItems: number;
}

export interface CurrentFolderResponse {
  folder: ITHit.WebDAV.Client.Folder;
  result: CurrentFolderResult;
  withSearch: boolean;
}

export class WebDavService {
  public static isFolder(item: ITHit.WebDAV.Client.HierarchyItem) {
    return item.ResourceType === ITHit.WebDAV.Client.ResourceType.Folder;
  }

  public static async fetchFolderItems(
    currentUrl: string,
    pageSize: number,
    sortColumn: string,
    sortAscending: boolean,
    page: number,
    withSearch: boolean,
    searchQuery?: string
  ): Promise<CurrentFolderResponse> {
    try {
      const response = await WebDavService.getCurrentFolder(
        currentUrl.replace(/\/?$/, "/")
      );
      const folder = response.Result as ITHit.WebDAV.Client.Folder;

      const itemsResponse =
        withSearch && searchQuery
          ? await WebDavService.getItemsByQuery(
            folder,
            page,
            pageSize,
            searchQuery
          )
          : await WebDavService.getItems(
            folder,
            sortColumn,
            sortAscending,
            page,
            pageSize
          );

      return {
        folder,
        result: {
          page: itemsResponse.Result.Page,
          totalItems: itemsResponse.Result.TotalItems,
        },
        withSearch,
      };
    } catch (err) {
      throw new WebDavError("Error fetching folder items", err);
    }
  }

  public static getCurrentFolder(sPath: string) {
    return new Promise<ITHit.WebDAV.Client.AsyncResult>((resolve, reject) => {
      webDavSession.OpenFolderAsync(sPath, [], (data) => {
        if (!data.IsSuccess) return reject(data.Error);
        resolve(data);
      });
    });
  }

  public static getItem(sPath: string) {
    return new Promise<ITHit.WebDAV.Client.AsyncResult>((resolve, reject) => {
      webDavSession.OpenItemAsync(sPath, [], (data) => {
        if (!data.IsSuccess) return reject(data.Error);
        resolve(data);
      });
    });
  }

  public static getItems(
    currentFolder: ITHit.WebDAV.Client.Folder,
    sortColumn: string,
    sortAscending: boolean,
    currentPage: number,
    pageSize: number
  ) {
    return new Promise<ITHit.WebDAV.Client.AsyncResult>((resolve, reject) => {
      currentFolder.GetPageAsync(
        [],
        (currentPage - 1) * pageSize,
        pageSize,
        this.setSortColumnsValue(sortColumn, sortAscending),
        (data) => {
          if (!data.IsSuccess) return reject(data.Error);
          resolve(data);
        }
      );
    });
  }

  public static getItemsByQuery(
    currentFolder: ITHit.WebDAV.Client.Folder,
    currentPage: number,
    pageSize: number,
    sPhrase: string
  ) {
    const searchQuery = new ITHit.WebDAV.Client.SearchQuery(
      sPhrase
        .replace(/\\/g, "\\\\")
        .replace(/\\%/g, "\\%")
        .replace(/\\_/g, "\\_")
        .replace(/\*/g, "%")
        .replace(/\?/g, "_") + "%"
    );
    searchQuery.SelectProperties = [
      new ITHit.WebDAV.Client.PropertyName("snippet", "ithit"),
    ];
    return new Promise<ITHit.WebDAV.Client.AsyncResult>((resolve, reject) => {
      currentFolder.GetSearchPageByQueryAsync(
        searchQuery,
        (currentPage - 1) * pageSize,
        pageSize,
        (data) => {
          if (!data.IsSuccess) return reject(data.Error);
          resolve(data);
        }
      );
    });
  }

  public static getSupportedFeatures(
    currentFolder: ITHit.WebDAV.Client.Folder
  ) {
    return new Promise<ITHit.WebDAV.Client.AsyncResult>((resolve, reject) => {
      currentFolder.GetSupportedFeaturesAsync((data) => {
        if (!data.IsSuccess) return reject(data.Error);
        resolve(data);
      });
    });
  }

  public static createFolder(
    currentFolder: ITHit.WebDAV.Client.Folder,
    folderName: string
  ) {
    return new Promise<ITHit.WebDAV.Client.AsyncResult>((resolve, reject) => {
      currentFolder.CreateFolderAsync(folderName, "", [], (data) => {
        if (!data.IsSuccess) return reject(data);
        resolve(data);
      });
    });
  }

  public static openItem(
    path: string,
    fCallback?: (oResult: ITHit.WebDAV.Client.AsyncResult) => void
  ) {
    webDavSession.OpenItemAsync(
      UrlResolveService.encodeUri(path),
      [],
      fCallback
    );
  }

  private static setSortColumnsValue(
    sortColumn: string,
    sortAscending: boolean
  ) {
    const namespaceUri = "DAV:";
    const sortColumns = [
      new ITHit.WebDAV.Client.OrderProperty(
        new ITHit.WebDAV.Client.PropertyName("is-directory", namespaceUri),
        false
      ),
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
}
