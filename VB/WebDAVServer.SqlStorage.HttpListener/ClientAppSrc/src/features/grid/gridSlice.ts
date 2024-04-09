import {
  createAsyncThunk,
  createSlice,
  PayloadAction,
  AnyAction,
} from "@reduxjs/toolkit";
import { RootState } from "../../app/store";
import { ITHit } from "webdav.client";
import { WebDavService } from "../../services/WebDavService";
import { UrlResolveService } from "../../services/UrlResolveService";
import { StoredType } from "../../models/StoredType";
import { WebDavError } from "../../models/WebDavError";
import { getI18n } from "react-i18next";
import { SearchParams } from "../../models/SearchParams";
import { CommonService } from "../../services/CommonService";
import {
  getMidIgnoredAction,
  getMidIgnoredPath,
  ReducerType,
} from "../../app/storeFunctions";
const i18n = getI18n();

const isRejectedAction = (action: AnyAction) => {
  return action.type.endsWith("rejected");
};

const isAllSelected = (state: GridState) => {
  return (
    state.selectedIndexes.length > 0 &&
    state.selectedIndexes.length === state.items.length
  );
};

interface CurrentFolderResult {
  folder: ITHit.WebDAV.Client.Folder;
  result: any;
  withSearch: boolean;
}

export interface GridState {
  sortAscending: boolean;
  sortColumn: string;
  searchQuery: string;
  searchMode: boolean;
  currentFolder: ITHit.WebDAV.Client.Folder | null;
  items: ITHit.WebDAV.Client.HierarchyItem[];
  searchedItems: ITHit.WebDAV.Client.HierarchyItem[];
  storedItems: ITHit.WebDAV.Client.HierarchyItem[];
  selectedIndexes: number[];
  loading: boolean;
  loadingWithSceleton: boolean;
  error: WebDavError | null;
  currentPage: number;
  pageSize: number;
  countPages: number;
  allSelected: boolean;
  currentUrl: string;
  storedType: StoredType;
  optionsInfo: ITHit.WebDAV.Client.OptionsInfo | null;
  optionsInfoLoading: boolean;
  protocolModalDisplayed: boolean;
}

export const initialState: GridState = {
  sortAscending: true,
  sortColumn: "displayname",
  searchQuery: "",
  searchMode: false,
  currentFolder: null,
  items: [],
  searchedItems: [],
  storedItems: [],
  selectedIndexes: [],
  loading: true,
  loadingWithSceleton: false,
  error: null,
  pageSize: 10,
  currentPage: 1,
  countPages: 0,
  allSelected: false,
  currentUrl: UrlResolveService.getRootUrl(),
  storedType: StoredType.Copy,
  optionsInfo: null,
  optionsInfoLoading: true,
  protocolModalDisplayed: false,
};

export const setCurrentFolder = createAsyncThunk(
  "grid/setCurrentFolder",
  async (withSearch: boolean, { getState, rejectWithValue }) => {
    const { grid } = getState() as { grid: GridState };
    const {
      currentPage,
      pageSize,
      sortColumn,
      sortAscending,
      currentUrl,
      searchQuery,
    } = grid;
    try {
      const response = await WebDavService.getCurrentFolder(currentUrl);
      if (response.IsSuccess) {
        let folder = response.Result as ITHit.WebDAV.Client.Folder;

        try {
          let itemsResponse;
          if (withSearch) {
            itemsResponse = await WebDavService.getItemsByQuery(
              folder,
              currentPage,
              pageSize,
              searchQuery
            );
          } else {
            itemsResponse = await WebDavService.getItems(
              folder,
              sortColumn,
              sortAscending == null ? false : sortAscending,
              currentPage,
              pageSize
            );
          }

          let currentFolderResult: CurrentFolderResult = {
            folder: folder,
            result: itemsResponse.Result,
            withSearch: withSearch,
          };
          return currentFolderResult as any;
        } catch (err) {
          return rejectWithValue(
            new WebDavError(i18n.t("phrases.errors.profindErrorMessage"), err)
          );
        }
      }
    } catch (err) {
      return rejectWithValue(
        new WebDavError(i18n.t("phrases.errors.profindErrorMessage"), err)
      );
    }
  }
);

export const setItem = createAsyncThunk(
  "grid/setItem",
  async (itemPath: string, { getState, rejectWithValue }) => {
    try {
      const { grid } = getState() as { grid: GridState };
      const { items } = grid;
      var item = items.find((p) => p.Href === itemPath);
      if (item && item.Href) {
        const response = await WebDavService.getItem(item.Href);
        if (response.IsSuccess) {
          return response.Result as ITHit.WebDAV.Client.HierarchyItem;
        }
      }
    } catch (err) {
      return rejectWithValue(err);
    }
  }
);

export const setOptionsInfo = createAsyncThunk(
  "grid/setOptionsInfo",
  async (_, { getState, rejectWithValue }) => {
    const { grid } = getState() as { grid: GridState };
    const { currentFolder } = grid;
    try {
      if (currentFolder) {
        const response = await WebDavService.getSupportedFeatures(
          currentFolder
        );
        return response.Result;
      }
      return null;
    } catch (err) {
      return rejectWithValue(new WebDavError("", err));
    }
  }
);

export const setSearchedItems = createAsyncThunk(
  "grid/setSearchedItems",
  async (searchParams: SearchParams, { getState, rejectWithValue }) => {
    const { grid } = getState() as { grid: GridState };
    const { currentFolder } = grid;
    try {
      if (currentFolder) {
        const response = await WebDavService.getItemsByQuery(
          currentFolder,
          searchParams.pageNumber,
          searchParams.pageSize,
          searchParams.query
        );
        return response.Result.Page;
      }
      return [];
    } catch (err) {
      return rejectWithValue(
        new WebDavError(i18n.t("phrases.errors.searchErrorMessage"), err)
      );
    }
  }
);

export const gridSlice = createSlice({
  name: "grid",
  initialState,
  reducers: {
    setSortColumn: (state, action: PayloadAction<string>) => {
      state.sortColumn = action.payload;
    },
    setSortAscending: (state, action: PayloadAction<boolean>) => {
      state.sortAscending = action.payload;
    },
    setCurrentPage: (state, action: PayloadAction<number>) => {
      state.currentPage = action.payload;
    },
    setSearchQuery: (state, action: PayloadAction<string>) => {
      state.searchQuery = action.payload;
      if (state.searchQuery) {
        state.searchMode = true;
      } else {
        state.searchMode = false;
      }
    },
    setSearchedItem: (
      state,
      action: PayloadAction<ITHit.WebDAV.Client.HierarchyItem>
    ) => {
      state.items = [action.payload];
      state.currentPage = 1;
      state.countPages = 1;
      state.searchMode = true;
    },
    setLoadingWithSceleton: (state, action: PayloadAction<boolean>) => {
      state.loadingWithSceleton = action.payload;
    },
    addSelectedItem: (state, action: PayloadAction<number>) => {
      state.selectedIndexes.push(action.payload);
      state.allSelected = isAllSelected(state);
    },
    removeSelectedItem: (state, action: PayloadAction<number>) => {
      state.selectedIndexes.forEach((selectedIndex, index) => {
        if (selectedIndex === action.payload) {
          state.selectedIndexes.splice(index, 1);
          return false;
        }
      });
      state.allSelected = isAllSelected(state);
    },
    clearSelectedItems: (state) => {
      state.selectedIndexes = [];
      state.allSelected = isAllSelected(state);
    },
    addAllSelectedItems: (state) => {
      if (state.items.length - 1 > 0) {
        state.selectedIndexes = Array.from(
          { length: state.items.length },
          (v, k) => k
        );
      } else {
        state.selectedIndexes = [];
      }
      state.allSelected = isAllSelected(state);
    },
    storeSelectedItems: (state, action: PayloadAction<StoredType>) => {
      state.storedItems = [...state.selectedIndexes.map((i) => state.items[i])];
      state.storedType = action.payload;
    },
    clearStoredItems: (state) => {
      state.storedItems = [];
    },
    setCurrentUrl: (state, action: PayloadAction<string>) => {
      state.currentUrl = action.payload;
    },
    clearItems: (state) => {
      state.items = [];
    },
    showProtocolModal: (state) => {
      state.protocolModalDisplayed = true;
    },
    hideProtocolModal: (state) => {
      state.protocolModalDisplayed = false;
    },
    setError: (state, action: PayloadAction<WebDavError>) => {
      state.error = action.payload;
    },
    clearError: (state) => {
      state.error = null;
    },
    clearSearchedItems: (state) => {
      state.searchedItems = [];
    },
  },
  extraReducers: (builder) => {
    builder.addCase(setCurrentFolder.fulfilled, (state, action) => {
      if (state.loading) state.loading = false;
      state.items = action.payload.result
        .Page as ITHit.WebDAV.Client.HierarchyItem[];
      state.countPages = Math.ceil(
        action.payload.result.TotalItems / state.pageSize
      );
      state.currentFolder = action.payload.folder;
      state.searchMode = action.payload.withSearch;
    });

    builder.addCase(setSearchedItems.fulfilled, (state, action) => {
      state.searchedItems =
        action.payload as ITHit.WebDAV.Client.HierarchyItem[];
    });

    builder.addCase(setCurrentFolder.pending, (state) => {
      if (state.loadingWithSceleton) state.loading = true;
    });

    builder.addCase(setCurrentFolder.rejected, (state) => {
      if (state.loading) state.loading = false;
    });

    builder.addCase(setItem.fulfilled, (state, action) => {
      let updatedItem = action.payload as ITHit.WebDAV.Client.HierarchyItem;
      let index = state.items.findIndex(
        (p) => p.DisplayName === updatedItem.DisplayName
      );
      if (index >= 0) {
        state.items[index] = updatedItem;
      }
    });

    builder.addCase(setOptionsInfo.fulfilled, (state, action) => {
      state.optionsInfo = action.payload as ITHit.WebDAV.Client.OptionsInfo;
    });

    builder.addMatcher(isRejectedAction, (state, action) => {
      if (action.payload instanceof WebDavError) {
        state.error = action.payload;
      } else if (action.payload instanceof ITHit.WebDAV.Client.Error) {
        state.error = new WebDavError(
          action.payload.Description,
          action.payload
        );
      } else if (
        action.payload instanceof ITHit.WebDAV.Client.Exceptions.WebDavException
      ) {
        state.error = new WebDavError(
          action.payload.Message.toString(),
          action.payload
        );
      }
    });
  },
});

export const {
  setSortColumn,
  setSortAscending,
  setCurrentPage,
  setSearchQuery,
  setSearchedItem,
  setLoadingWithSceleton,
  addSelectedItem,
  removeSelectedItem,
  clearSelectedItems,
  addAllSelectedItems,
  storeSelectedItems,
  setCurrentUrl,
  clearStoredItems,
  clearItems,
  showProtocolModal,
  hideProtocolModal,
  setError,
  clearError,
  clearSearchedItems,
} = gridSlice.actions;

// export getters
export const getSortAscending = (state: RootState) => state.grid.sortAscending;
export const getSortColumn = (state: RootState) => state.grid.sortColumn;
export const getItems = (state: RootState) => state.grid.items;
export const getPageSize = (state: RootState) => state.grid.pageSize;
export const getCountPages = (state: RootState) => state.grid.countPages;
export const getCurrentPage = (state: RootState) => state.grid.currentPage;
export const getSearchQuery = (state: RootState) => state.grid.searchQuery;
export const getSearchMode = (state: RootState) => state.grid.searchMode;
export const getSelectedIndexes = (state: RootState) =>
  state.grid.selectedIndexes;
export const getSelectedItems = (state: RootState) =>
  state.grid.selectedIndexes.map((i) => state.grid.items[i]);
export const getAllSelected = (state: RootState) => state.grid.allSelected;
export const getLoading = (state: RootState) => state.grid.loading;
export const getCurrentFolder = (state: RootState) => state.grid.currentFolder;
export const getSearchedItems = (state: RootState) => state.grid.searchedItems;
export const getStoredItems = (state: RootState) => state.grid.storedItems;
export const getStoredType = (state: RootState) => state.grid.storedType;
export const getCurrentUrl = (state: RootState) => state.grid.currentUrl;
export const getOptionsInfo = (state: RootState) => state.grid.optionsInfo;
export const getError = (state: RootState) => state.grid.error;
export const getProtocolModalDisplayed = (state: RootState) =>
  state.grid.protocolModalDisplayed;

export const gridMidIgnoredActions = [
  getMidIgnoredAction(setSearchedItems.typePrefix, ReducerType.pending),
  getMidIgnoredAction(setSearchedItems.typePrefix, ReducerType.rejected),
  getMidIgnoredAction(setSearchedItems.typePrefix, ReducerType.fulfilled),
  getMidIgnoredAction(setCurrentFolder.typePrefix, ReducerType.rejected),
  getMidIgnoredAction(setCurrentFolder.typePrefix, ReducerType.fulfilled),
  getMidIgnoredAction(setItem.typePrefix, ReducerType.rejected),
  getMidIgnoredAction(setItem.typePrefix, ReducerType.fulfilled),
  getMidIgnoredAction(setOptionsInfo.typePrefix, ReducerType.rejected),
  getMidIgnoredAction(setOptionsInfo.typePrefix, ReducerType.fulfilled),
];

export const gridMidIgnoredPaths = [
  getMidIgnoredPath(
    gridSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.items)
  ),
  getMidIgnoredPath(
    gridSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.searchedItems)
  ),
  getMidIgnoredPath(
    gridSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.storedItems)
  ),
  getMidIgnoredPath(
    gridSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.error)
  ),
  getMidIgnoredPath(
    gridSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.currentFolder)
  ),
  getMidIgnoredPath(
    gridSlice.name,
    CommonService.getPropertyName(initialState, (x) => x.optionsInfo)
  ),
];

export default gridSlice.reducer;
