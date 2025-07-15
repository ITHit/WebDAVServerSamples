import { store } from "./store";
import {
  GridState,
  initialState,
  setCurrentUrl,
  setCurrentFolder,
  clearSelectedItems,
  clearStoredItems,
  setOptionsInfo,
  setError,
  setCurrentPage,
  setSortColumn,
  setSortAscending,
  setSearchQuery,
  setItem,
  showProtocolModal,
  setLoadingWithSkeleton,
  refreshFolder,
} from "../features/grid/gridSlice";
import { StoredType } from "../models/StoredType";
import { ITHit } from "webdav.client";
import { WebDavError } from "../models/WebDavError";
import { getI18n } from "react-i18next";
import { CommonService } from "../services/CommonService";
import { WebDavService } from "../services/WebDavService";
import { WebDavSettings } from "../webDavSettings";
import { UrlResolveService } from "../services/UrlResolveService";
import { createBrowserHistory } from "history";
import { NavigateFunction } from "react-router-dom";
import { QueryParams } from "../models/QueryParams";
import { UploadService } from "../services/UploadService";
import { getSelectedItems } from "./storeSelectors";
const i18n = getI18n();
const history = createBrowserHistory();

export class StoreWorker {
  static navigate: NavigateFunction;

  static setNavigate(navigateFunction: NavigateFunction) {
    this.navigate = navigateFunction;
  }

  static refresh(
    url?: string | null,
    queryParams?: QueryParams | null,
    showSkeleton?: boolean
  ) {
    const { searchMode, searchQuery } = this._getGrid();
    let curSearchMode = searchMode;
    let location = "";
    if (url) {
      store.dispatch(setCurrentUrl(url));
      UploadService.setUploadUrl(url);
      const tail = UrlResolveService.getTail(
        url,
        UrlResolveService.getOrigin()
      );
      if (tail) {
        location = tail;
      } else {
        location = "/";
      }
      store.dispatch(setCurrentPage(1));
    } else {
      location = window.location.pathname;
    }

    if (queryParams) {
      this._setHash(queryParams);
      if (queryParams.search) {
        curSearchMode = true;
      } else {
        curSearchMode = false;
      }
    } else {
      if (!searchQuery && searchMode) {
        curSearchMode = false;
      }
    }

    if (curSearchMode !== searchMode) {
      store.dispatch(setCurrentPage(1));
    }
    store.dispatch(clearSelectedItems());
    store.dispatch(setLoadingWithSkeleton(!!showSkeleton));
    store.dispatch(setCurrentFolder(curSearchMode)).then(() => {
      store.dispatch(setOptionsInfo());
      if (this.navigate) {
        this.navigate(location + this._getHash());
      } else {
        history.push(location + this._getHash());
      }
    });
  }

  static updateItem(itemName: string) {
    store.dispatch(setItem(itemName));
  }

  static async getSerachedItems(query: string, pageSize: number) {
    const { currentFolder } = this._getGrid();
    let items: ITHit.WebDAV.Client.HierarchyItem[] = [];
    if (currentFolder) {
      try {
        const response = await WebDavService.getItemsByQuery(
          currentFolder,
          1,
          pageSize,
          query
        );

        items = response.Result;
        return items;
      } catch (err) {
        store.dispatch(
          setError(
            new WebDavError(i18n.t("phrases.errors.searchErrorMessage"), err)
          )
        );
        return [];
      }
    }
  }

  static refreshFolder(tryParent: boolean | undefined) {
    store.dispatch(refreshFolder(tryParent));
  }

  static pasteStoredItems() {
    const { storedType } = this._getGrid();
    switch (storedType) {
      case StoredType.Cut:
        this._moveItems();
        break;
      case StoredType.Copy:
        this._copyItems();
        break;
    }

    store.dispatch(clearStoredItems());
  }

  static deleteSelectedItems() {
    const selectedItems = getSelectedItems(store.getState());

    selectedItems.forEach((item) => {
      item.DeleteAsync(null, (data) => {
        if (data.IsSuccess) {
          this.refresh();
        } else {
          const error = new WebDavError(
            i18n.t("phrases.errors.deletetemsErrorMessage"),
            data.Error
          );
          store.dispatch(setError(error));
        }
      });
    });
  }

  static renameSelectedItem(name: string) {
    const selectedItems = getSelectedItems(store.getState());
    const { currentFolder } = this._getGrid();

    if (selectedItems.length === 1 && currentFolder !== null) {
      selectedItems[0].MoveToAsync(currentFolder, name, false, [], (data) => {
        if (data.IsSuccess) {
          this.refresh();
        } else {
          let error;
          if (
            data.Error instanceof ITHit.WebDAV.Client.Exceptions.LockedException
          ) {
            error = new WebDavError(
              i18n.t("phrases.errors.renameItemLockedErrorMessage"),
              data.Error
            );
          } else {
            error = new WebDavError(
              i18n.t("phrases.errors.renameItemErrorMessage"),
              data.Error
            );
          }
          store.dispatch(setError(error));
        }
      });
    }
  }

  static printSelectedItems() {
    const selectedItems = getSelectedItems(store.getState());

    const filesUrls: string[] = [];
    selectedItems.forEach((item) => {
      if (!WebDavService.isFolder(item)) {
        filesUrls.push(item.Href);
      }
    });

    ITHit.WebDAV.Client.DocManager.DavProtocolEditDocument(
      filesUrls,
      UrlResolveService.getOrigin(),
      () => {
        store.dispatch(showProtocolModal());
      },
      null,
      WebDavSettings.EditDocAuth.SearchIn,
      WebDavSettings.EditDocAuth.CookieNames,
      WebDavSettings.EditDocAuth.LoginUrl,
      "Print"
    );
  }

  private static _moveItems() {
    const { storedItems, currentFolder } = this._getGrid();

    storedItems.forEach((item) => {
      if (currentFolder !== null)
        item.MoveToAsync(currentFolder, item.DisplayName, false, [], (data) => {
          if (data.IsSuccess) {
            this.refresh();
          } else {
            let error;
            if (
              data.Error instanceof
              ITHit.WebDAV.Client.Exceptions.ForbiddenException
            ) {
              error = new WebDavError(
                i18n.t("phrases.errors.cutItemsSameNameErrorMessage"),
                data.Error
              );
            } else if (
              data.Error instanceof
              ITHit.WebDAV.Client.Exceptions.LockedException
            ) {
              error = new WebDavError(
                i18n.t("phrases.errors.cutItemsLockedErrorMessage"),
                data.Error
              );
            } else {
              error = new WebDavError(
                i18n.t("phrases.errors.cutItemsErrorMessage"),
                data.Error
              );
            }
            store.dispatch(setError(error));
          }
        });
    });
  }

  private static _copyItems() {
    const { storedItems, currentFolder } = this._getGrid();
    storedItems.forEach((item) => {
      const itemCopyName = CommonService.getCopySuffix(item.DisplayName, false);
      if (currentFolder !== null) {
        this._copyItem(item, itemCopyName, currentFolder);
      }
    });
  }

  private static _copyItem(
    item: ITHit.WebDAV.Client.HierarchyItem,
    copyName: string,
    folder: ITHit.WebDAV.Client.Folder
  ) {
    item.CopyToAsync(folder, copyName, true, false, [], (data) => {
      if (data.IsSuccess) {
        this.refresh();
      } else {
        if (
          data.Error instanceof
          ITHit.WebDAV.Client.Exceptions.PreconditionFailedException ||
          data.Error instanceof
          ITHit.WebDAV.Client.Exceptions.ForbiddenException
        ) {
          const newCopyName = CommonService.getCopySuffix(copyName, true);
          this._copyItem(item, newCopyName, folder);
        } else {
          const error = new WebDavError(
            i18n.t("phrases.errors.copyItemsErrorMessage"),
            data.Error
          );
          store.dispatch(setError(error));
        }
      }
    });
  }

  private static _getHash() {
    const { currentPage, sortColumn, sortAscending, searchQuery } =
      this._getGrid();
    const hashArray = [];
    if (currentPage !== initialState.currentPage) {
      hashArray.push(`page=${currentPage}`);
    }
    if (sortColumn !== initialState.sortColumn) {
      hashArray.push(`sortcolumn=${sortColumn}`);
    }
    if (sortAscending !== initialState.sortAscending) {
      hashArray.push(`sortascending=${sortAscending}`);
    }
    if (searchQuery !== initialState.searchQuery) {
      hashArray.push(`search=${searchQuery}`);
    }

    return hashArray.length ? "?" + hashArray.join("&") : "";
  }

  private static _setHash(queryParams: QueryParams) {
    store.dispatch(setCurrentPage(queryParams.page));
    store.dispatch(setSortColumn(queryParams.sortcolumn));
    store.dispatch(setSortAscending(queryParams.sortascending));
    store.dispatch(setSearchQuery(queryParams.search));
  }

  private static _getGrid() {
    const { grid } = store.getState() as { grid: GridState };
    return grid;
  }
}
