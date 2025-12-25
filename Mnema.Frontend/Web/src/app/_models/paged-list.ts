
export type PagedList<T> = {
  items: T[];
  currentPage: number;
  totalPages: number;
  pageSize: number;
  totalCount: number;
}

export const EMPTY_PAGE: PagedList<any> = {
  items: [],
  totalPages: 0,
  currentPage: 0,
  pageSize: 0,
  totalCount: 0,
};
