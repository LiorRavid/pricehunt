import { httpResource, type HttpResourceRef } from '@angular/common/http';
import { Injectable, type Signal } from '@angular/core';
import type { HistoryPage } from '../domain/history-item';
import type { HistoryQuery } from '../domain/history-query';
import type { HistoryPageDto } from './history-dtos';
import { toHistoryPage } from './history-page-mapper';
import { toHistoryParams } from './history-params';

/** Reads the price history from the API, which does all filtering, sorting and paging (H1, H2). */
@Injectable({ providedIn: 'root' })
export class HistoryApi {
  /**
   * Loads the page `query` describes, and again whenever it changes, cancelling a request that has
   * been superseded. Call it in an injection context, which then owns the resource.
   */
  load(query: Signal<HistoryQuery>): HttpResourceRef<HistoryPage | undefined> {
    return httpResource<HistoryPage>(
      () => ({ url: '/api/history', params: toHistoryParams(query()) }),
      { parse: (body) => toHistoryPage(body as HistoryPageDto) },
    );
  }
}
