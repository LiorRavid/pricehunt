import { HttpParams } from '@angular/common/http';
import { addDays, startOfLocalDay } from '../../../shared/dates/local-date';
import type { HistoryQuery } from '../domain/history-query';

/**
 * The `GET /api/history` query for a history view (H1, H2). The local days become a half-open UTC
 * range, from local midnight on the first day to local midnight after the last one, and only the
 * filters in use are sent.
 */
export function toHistoryParams(query: HistoryQuery): HttpParams {
  let params = new HttpParams()
    .set('startDate', startOfLocalDay(query.from).toISOString())
    .set('endDate', startOfLocalDay(addDays(query.to, 1)).toISOString());

  for (const supplierId of query.suppliers) {
    params = params.append('suppliers', supplierId);
  }

  const origin = query.origin.trim();
  if (origin) {
    params = params.set('origin', origin);
  }

  const destination = query.destination.trim();
  if (destination) {
    params = params.set('destination', destination);
  }

  if (query.includeFailures) {
    params = params.set('includeFailures', true);
  }

  return params
    .set('sortBy', query.sortBy)
    .set('sortDirection', query.sortDirection)
    .set('page', query.page)
    .set('pageSize', query.pageSize);
}
