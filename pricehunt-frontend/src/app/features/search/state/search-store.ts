import { computed, inject, Injectable, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { catchError, EMPTY, map, type Observable, of, Subject, switchMap } from 'rxjs';
import { SearchApi } from '../data-access/search-api';
import { sortResultRows } from '../domain/result-row';
import type { SearchCriteria, SupplierRef } from '../domain/search-criteria';
import { describeSearchError } from './search-error';
import { searchReducer, type SearchAction } from './search-reducer';
import { initialSearchState } from './search-state';
import { selectBadge, selectProgress } from './search-view';

interface SearchRequest {
  readonly attempt: number;
  readonly criteria: SearchCriteria;
}

/**
 * The live search's state, scoped to the search page (ADR-004). Searches run through `switchMap`, so
 * starting a new one, cancelling, or leaving the page unsubscribes from the old stream, which aborts
 * its request and cancels it on the server.
 */
@Injectable()
export class SearchStore {
  private readonly api = inject(SearchApi);
  private readonly state = signal(initialSearchState);
  private readonly requests = new Subject<SearchRequest | null>();

  readonly phase = computed(() => this.state().phase);
  readonly searching = computed(() => this.state().phase === 'searching');
  readonly attempt = computed(() => this.state().attempt);
  readonly rows = computed(() => sortResultRows(this.state().rows));
  readonly progress = computed(() => selectProgress(this.state()));
  readonly badge = computed(() => selectBadge(this.state()));
  readonly maxDurationMs = computed(() => this.state().maxDurationMs);

  constructor() {
    this.requests
      .pipe(
        switchMap((request) => (request ? this.run(request) : EMPTY)),
        takeUntilDestroyed(),
      )
      .subscribe((action) => {
        this.dispatch(action);
      });
  }

  /** Starts a search, replacing (and cancelling) any search in flight. */
  search(criteria: SearchCriteria, suppliers: readonly SupplierRef[]): void {
    const attempt = this.state().attempt + 1;
    this.dispatch({ type: 'searchRequested', attempt, suppliers });
    this.requests.next({ attempt, criteria });
  }

  /** Stops the search in flight, if any. */
  cancel(): void {
    if (!this.searching()) {
      return;
    }

    this.dispatch({ type: 'searchCancelled', attempt: this.state().attempt });
    this.requests.next(null);
  }

  private run({ attempt, criteria }: SearchRequest): Observable<SearchAction> {
    return this.api.stream(criteria).pipe(
      map((event): SearchAction => ({ type: 'eventReceived', attempt, event })),
      catchError((error: unknown) =>
        of<SearchAction>({ type: 'streamFailed', attempt, message: describeSearchError(error) }),
      ),
    );
  }

  private dispatch(action: SearchAction): void {
    this.state.update((state) => searchReducer(state, action));
  }
}
