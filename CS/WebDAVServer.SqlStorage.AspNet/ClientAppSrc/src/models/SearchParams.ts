export class SearchParams {
  query: string;
  pageSize: number;
  pageNumber: number;

  constructor(query: string, pageSize: number, pageNumber: number = 1) {
    this.query = query;
    this.pageSize = pageSize;
    this.pageNumber = pageNumber;
  }
}
