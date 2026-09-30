import type { ParamMap } from '@angular/router';
import { addDays, isIsoDate } from '../../../shared/dates/local-date';
import {
  DEFAULT_PAGE_SIZE,
  defaultSortDirection,
  HISTORY_SORT_FIELDS,
  PAGE_SIZES,
  type HistoryQuery,
  type SortDirection,
} from '../domain/history-query';

/** How many days the history shows by default, today included. */
const DEFAULT_DAYS = 7;

/** The API's limit for a location filter. */
const MAX_TEXT_LENGTH = 100;

const SORT_DIRECTIONS: readonly SortDirection[] = ['asc', 'desc'];
const SUPPLIER_ID = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

/** Query parameters for `Router.navigate`; a repeated parameter is an array. */
export type HistoryQueryParams = Record<string, string | string[]>;

/** The view shown when the URL says nothing: the last 7 days, every supplier, newest first. */
export function defaultHistoryQuery(today: string): HistoryQuery {
  return {
    from: addDays(today, 1 - DEFAULT_DAYS),
    to: today,
    suppliers: [],
    origin: '',
    destination: '',
    includeFailures: false,
    sortBy: 'date',
    sortDirection: defaultSortDirection('date'),
    page: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  };
}

/** The history view a URL describes (HC1, HC2). Anything missing or invalid takes its default. */
export function readHistoryQuery(params: ParamMap, today: string): HistoryQuery {
  const defaults = defaultHistoryQuery(today);
  const from = readDate(params.get('from')) ?? defaults.from;
  const to = readDate(params.get('to')) ?? defaults.to;
  const sortBy = oneOf(params.get('sort'), HISTORY_SORT_FIELDS) ?? defaults.sortBy;
  const size = Number(params.get('size'));

  return {
    from: from <= to ? from : to,
    to: from <= to ? to : from,
    suppliers: [...new Set(params.getAll('suppliers').filter((id) => SUPPLIER_ID.test(id)))],
    origin: (params.get('origin') ?? '').slice(0, MAX_TEXT_LENGTH),
    destination: (params.get('destination') ?? '').slice(0, MAX_TEXT_LENGTH),
    includeFailures: params.get('failures') === 'true',
    sortBy,
    sortDirection: oneOf(params.get('dir'), SORT_DIRECTIONS) ?? defaultSortDirection(sortBy),
    page: readPage(params.get('page')) ?? 1,
    pageSize: PAGE_SIZES.includes(size) ? size : DEFAULT_PAGE_SIZE,
  };
}

/** The query parameters for a view, leaving out whatever matches the default so links stay short. */
export function writeHistoryQuery(query: HistoryQuery, today: string): HistoryQueryParams {
  const defaults = defaultHistoryQuery(today);
  const params: HistoryQueryParams = {};
  // Both dates or neither, so a shared link keeps its range.
  if (query.from !== defaults.from || query.to !== defaults.to) {
    params['from'] = query.from;
    params['to'] = query.to;
  }
  if (query.suppliers.length > 0) {
    params['suppliers'] = [...query.suppliers];
  }
  if (query.origin) {
    params['origin'] = query.origin;
  }
  if (query.destination) {
    params['destination'] = query.destination;
  }
  if (query.includeFailures) {
    params['failures'] = 'true';
  }
  if (query.sortBy !== defaults.sortBy) {
    params['sort'] = query.sortBy;
  }
  if (query.sortDirection !== defaultSortDirection(query.sortBy)) {
    params['dir'] = query.sortDirection;
  }
  if (query.page !== 1) {
    params['page'] = String(query.page);
  }
  if (query.pageSize !== DEFAULT_PAGE_SIZE) {
    params['size'] = String(query.pageSize);
  }
  return params;
}

function readDate(value: string | null): string | undefined {
  return value !== null && isIsoDate(value) ? value : undefined;
}

function readPage(value: string | null): number | undefined {
  const page = Number(value);
  return value !== null && Number.isInteger(page) && page >= 1 ? page : undefined;
}

function oneOf<T extends string>(value: string | null, options: readonly T[]): T | undefined {
  return options.find((option) => option === value);
}
