/** Columns the history can be sorted by, named as the API names them. */
export type HistorySortField = 'date' | 'route' | 'supplier' | 'price' | 'responseTime';

export type SortDirection = 'asc' | 'desc';

export const HISTORY_SORT_FIELDS: readonly HistorySortField[] = [
  'date',
  'route',
  'supplier',
  'price',
  'responseTime',
];

/** The page sizes on offer; the API allows up to 100. */
export const PAGE_SIZES: readonly number[] = [10, 20, 50, 100];

export const DEFAULT_PAGE_SIZE = 20;

/**
 * What the history screen shows: its filters, sort and page (HC1, HC2). The URL holds all of it.
 * Dates are local calendar days (`yyyy-MM-dd`), both included.
 */
export interface HistoryQuery {
  readonly from: string;
  readonly to: string;
  /** Supplier ids to include; empty means every supplier. */
  readonly suppliers: readonly string[];
  readonly origin: string;
  readonly destination: string;
  /** Whether failed, timed-out and cancelled responses are shown alongside the quotes. */
  readonly includeFailures: boolean;
  readonly sortBy: HistorySortField;
  readonly sortDirection: SortDirection;
  /** 1-based. */
  readonly page: number;
  readonly pageSize: number;
}

/** The direction a column sorts in when it's first chosen: newest first for dates, else ascending. */
export function defaultSortDirection(field: HistorySortField): SortDirection {
  return field === 'date' ? 'desc' : 'asc';
}

/** Whether two queries ask for the same page of the same results. */
export function sameHistoryQuery(a: HistoryQuery, b: HistoryQuery): boolean {
  return (
    a.from === b.from &&
    a.to === b.to &&
    a.suppliers.length === b.suppliers.length &&
    a.suppliers.every((id, index) => id === b.suppliers[index]) &&
    a.origin === b.origin &&
    a.destination === b.destination &&
    a.includeFailures === b.includeFailures &&
    a.sortBy === b.sortBy &&
    a.sortDirection === b.sortDirection &&
    a.page === b.page &&
    a.pageSize === b.pageSize
  );
}

/** Sorts by `field`, flipping the direction when it's already the sort column, from page 1. */
export function toggleSort(query: HistoryQuery, field: HistorySortField): HistoryQuery {
  const sortDirection =
    query.sortBy === field
      ? query.sortDirection === 'asc'
        ? 'desc'
        : 'asc'
      : defaultSortDirection(field);
  return { ...query, sortBy: field, sortDirection, page: 1 };
}
