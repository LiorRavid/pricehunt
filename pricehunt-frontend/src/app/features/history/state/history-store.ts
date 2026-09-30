import { computed, effect, inject, Injectable, linkedSignal, untracked } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { debounceTime, Subject } from 'rxjs';
import { isIsoDate, toLocalIsoDate } from '../../../shared/dates/local-date';
import { HistoryApi } from '../data-access/history-api';
import type { HistoryPage } from '../domain/history-item';
import {
  sameHistoryQuery,
  toggleSort,
  type HistoryQuery,
  type HistorySortField,
} from '../domain/history-query';
import { summarizePage } from '../domain/page-summary';
import { describeHistoryError } from './history-error';
import { readHistoryQuery, writeHistoryQuery } from './history-url';

/** How long the location filters wait for typing to pause before the view follows. */
export const TEXT_FILTER_DELAY_MS = 300;

/**
 * The history screen's state (HC1, HC2), scoped to the page. The URL is the single source of
 * truth: every change navigates, and the view, including the request to the API, follows the URL.
 * So links can be shared, and back, forward and reload restore the view.
 */
@Injectable()
export class HistoryStore {
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  /** Today as a local calendar day: the latest a quote can be from. */
  readonly today = toLocalIsoDate(new Date());

  private readonly params = toSignal(this.route.queryParamMap, { requireSync: true });

  /** The view the URL describes. */
  readonly query = computed(() => readHistoryQuery(this.params(), this.today), {
    equal: sameHistoryQuery,
  });

  private readonly results = inject(HistoryApi).load(this.query);

  /**
   * The latest page that loaded, kept on screen while the next one loads so the table doesn't
   * flash. Like any linked signal it remembers what it last computed, which the view reads on
   * every render.
   */
  readonly page = linkedSignal<HistoryPage | undefined, HistoryPage | undefined>({
    source: () => (this.results.hasValue() ? this.results.value() : undefined),
    computation: (latest, previous) => latest ?? previous?.value,
  });

  readonly loading = this.results.isLoading;
  readonly errorMessage = computed(() => {
    const error = this.results.error();
    return error ? describeHistoryError(error) : null;
  });
  readonly summary = computed(() => {
    const { page, pageSize } = this.query();
    return summarizePage(page, pageSize, this.page()?.totalCount ?? 0);
  });

  /** What the location boxes show: the URL's text, or what's being typed before it gets there. */
  readonly originText = linkedSignal(() => this.query().origin);
  readonly destinationText = linkedSignal(() => this.query().destination);

  private readonly typing = new Subject<void>();

  constructor() {
    this.typing.pipe(debounceTime(TEXT_FILTER_DELAY_MS), takeUntilDestroyed()).subscribe(() => {
      // Typing replaces the history entry instead of adding one per pause.
      this.show(
        { origin: this.originText(), destination: this.destinationText(), page: 1 },
        { replaceUrl: true },
      );
    });

    // A page past the end, from an old link or a shrinking result, moves to the last page.
    effect(() => {
      const loaded = this.results.hasValue() ? this.results.value() : undefined;
      if (loaded) {
        const { pageCount } = summarizePage(loaded.page, loaded.pageSize, loaded.totalCount);
        if (loaded.page > pageCount) {
          untracked(() => {
            this.show({ page: pageCount }, { replaceUrl: true });
          });
        }
      }
    });
  }

  /** Moves the start of the range, taking the end along if it passes it; ignores invalid dates. */
  setFrom(from: string): void {
    if (isIsoDate(from)) {
      const { to } = this.query();
      this.show({ from, to: from > to ? from : to, page: 1 });
    }
  }

  /** Moves the end of the range, taking the start along if it passes it; ignores invalid dates. */
  setTo(to: string): void {
    if (isIsoDate(to)) {
      const { from } = this.query();
      this.show({ from: to < from ? to : from, to, page: 1 });
    }
  }

  setSuppliers(suppliers: readonly string[]): void {
    this.show({ suppliers, page: 1 });
  }

  typeOrigin(text: string): void {
    this.originText.set(text);
    this.typing.next();
  }

  typeDestination(text: string): void {
    this.destinationText.set(text);
    this.typing.next();
  }

  setIncludeFailures(includeFailures: boolean): void {
    this.show({ includeFailures, page: 1 });
  }

  sortBy(field: HistorySortField): void {
    this.show(toggleSort(this.query(), field));
  }

  goToPage(page: number): void {
    this.show({ page });
  }

  setPageSize(pageSize: number): void {
    this.show({ pageSize, page: 1 });
  }

  retry(): void {
    this.results.reload();
  }

  private show(change: Partial<HistoryQuery>, options: { replaceUrl?: boolean } = {}): void {
    void this.router.navigate([], {
      relativeTo: this.route,
      queryParams: writeHistoryQuery({ ...this.query(), ...change }, this.today),
      replaceUrl: options.replaceUrl ?? false,
    });
  }
}
