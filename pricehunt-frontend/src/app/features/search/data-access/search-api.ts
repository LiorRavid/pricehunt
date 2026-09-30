import { inject, Injectable } from '@angular/core';
import { filter, map, type Observable } from 'rxjs';
import { SseClient } from '../../../core/sse/sse-client';
import type { SearchCriteria } from '../domain/search-criteria';
import type { SearchEvent } from '../domain/search-event';
import type { StartSearchRequestDto } from './search-dtos';
import { SEARCH_COMPLETED, toSearchEvent } from './search-event-mapper';

/** Starts searches on the API and streams their events (ADR-001). */
@Injectable({ providedIn: 'root' })
export class SearchApi {
  private readonly sse = inject(SseClient);

  /** A cold stream: subscribing starts the search, unsubscribing cancels it on the server. */
  stream(criteria: SearchCriteria): Observable<SearchEvent> {
    const request: StartSearchRequestDto = {
      origin: criteria.origin.trim(),
      destination: criteria.destination.trim(),
      fromDate: criteria.fromDate,
      toDate: criteria.toDate,
      supplierIds: [...criteria.supplierIds],
    };

    return this.sse
      .post('/api/searches', request, (message) => message.event === SEARCH_COMPLETED)
      .pipe(
        map(toSearchEvent),
        filter((event): event is SearchEvent => event !== null),
      );
  }
}
