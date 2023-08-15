import { CommonService } from "../services/CommonService";
import { initialState } from "../features/grid/gridSlice";
export class QueryParams {
  page: number;
  search: string;
  sortcolumn: string;
  sortascending: boolean;
  constructor(params: any) {
    this.page =
      params.page && parseInt(params.page)
        ? parseInt(params.page)
        : initialState.currentPage;
    this.search = params.search ? params.search : initialState.searchQuery;
    this.sortcolumn = params.sortcolumn
      ? params.sortcolumn
      : initialState.sortColumn;
    this.sortascending = params.sortascending
      ? CommonService.stringToBoolean(params.sortascending)
      : initialState.sortAscending;
  }
}
