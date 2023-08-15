import { store } from "./store";
import {
  GridState,
  initialState,
  setCurrentUrl,
  setCurrentFolder,
  clearSelectedItems,
  clearStoredItems,
  getSelectedItems,
  clearItems,
  setOptionsInfo,
  setError,
  setCurrentPage,
  setSortColumn,
  setSortAscending,
  setSearchQuery,
  setItem,
  showProtocolModal,
} from "../features/grid/gridSlice";
import { StoredType } from "../models/StoredType";
import { ITHit } from "webdav.client";
import { WebDavError } from "../models/WebDavError";
import { getI18n } from "react-i18next";
import { CommonService } from "../services/CommonService";
import { WebDavService } from "../services/WebDavService";
import { WebDavSettings } from "../webDavSettings";
import { UrlResolveService } from "../services/UrlResolveService";
import { push } from "connected-react-router";
import { QueryParams } from "../models/QueryParams";
import { UploadService } from "../services/UploadService";
const i18n = getI18n();

export class StoreWorker {
  static refresh(url?: string, queryParams?: QueryParams) {
    const { searchMode, searchQuery } = this._getGrid();
    let curSearchMode = searchMode;
    let location = "";
    if (!!url) {
      store.dispatch(setCurrentUrl(url));
      UploadService.setUploadUrl(url);
      let tail = UrlResolveService.getTail(url, UrlResolveService.getOrigin());
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
    store.dispatch(clearItems());
    store.dispatch(setCurrentFolder(curSearchMode)).then(() => {
      store.dispatch(setOptionsInfo());
      store.dispatch(push(location + this._getHash()));
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
        let response = await WebDavService.getItemsByQuery(
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
    let selectedItems = getSelectedItems(store.getState());

    selectedItems.forEach((item) => {
      item.DeleteAsync(null, (data) => {
        if (data.IsSuccess) {
          this.refresh();
        } else {
          let error = new WebDavError(
            i18n.t("phrases.erros.deletetemsErrorMessage"),
            data.Error
          );
          store.dispatch(setError(error));
        }
      });
    });
  }

  static renameSelectedItem(name: string) {
    let selectedItems = getSelectedItems(store.getState());
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
              i18n.t("phrases.erros.renameItemLockedErrorMessage"),
              data.Error
            );
          } else {
            error = new WebDavError(
              i18n.t("phrases.erros.renameItemErrorMessage"),
              data.Error
            );
          }
          store.dispatch(setError(error));
        }
      });
    }
  }

  static printSelectedItems() {
    let selectedItems = getSelectedItems(store.getState());

    let filesUrls: string[] = [];
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
            var error;
            if (
              data.Error instanceof
              ITHit.WebDAV.Client.Exceptions.ForbiddenException
            ) {
              error = new WebDavError(
                i18n.t("phrases.erros.cutItemsSameNameErrorMessage"),
                data.Error
              );
            } else if (
              data.Error instanceof
              ITHit.WebDAV.Client.Exceptions.LockedException
            ) {
              error = new WebDavError(
                i18n.t("phrases.erros.cutItemsLockedErrorMessage"),
                data.Error
              );
            } else {
              error = new WebDavError(
                i18n.t("phrases.erros.cutItemsErrorMessage"),
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
      var itemCopyName = CommonService.getCopySuffix(item.DisplayName, false);
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
    item.CopyToAsync(folder, copyName, false, false, [], (data) => {
      if (data.IsSuccess) {
        this.refresh();
      } else {
        if (
          data.Error instanceof
            ITHit.WebDAV.Client.Exceptions.PreconditionFailedException ||
          data.Error instanceof
            ITHit.WebDAV.Client.Exceptions.ForbiddenException
        ) {
          let newCopyName = CommonService.getCopySuffix(copyName, true);
          this._copyItem(item, newCopyName, folder);
        } else {
          let error = new WebDavError(
            i18n.t("phrases.erros.copyItemsErrorMessage"),
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
    let hashArray = [];
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
