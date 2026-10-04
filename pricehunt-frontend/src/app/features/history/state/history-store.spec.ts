import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  type TestRequest,
} from '@angular/common/http/testing';
import { ApplicationRef, Component, inject } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { NavigationEnd, provideRouter, Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { filter, firstValueFrom } from 'rxjs';
import { addDays, toLocalIsoDate } from '../../../shared/dates/local-date';
import type { HistoryPageDto } from '../data-access/history-dtos';
import { HistoryStore, TEXT_FILTER_DELAY_MS } from './history-store';

@Component({ template: '', providers: [HistoryStore] })
class HistoryHost {
  readonly store = inject(HistoryStore);
}

const page = (overrides: Partial<HistoryPageDto> = {}): HistoryPageDto => ({
  items: [
    {
      id: 'r1',
      searchId: 's1',
      receivedAt: '2026-09-30T14:45:05Z',
      origin: 'Haifa',
      destination: 'Rotterdam',
      shipDateFrom: '2026-09-30',
      shipDateTo: '2026-10-07',
      supplierId: 'albatross-freight',
      supplierName: 'Albatross Freight',
      outcome: 'Succeeded',
      price: { amount: 1684.43, currency: 'USD' },
      responseTimeMs: 960,
      errorCode: null,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 1,
  ...overrides,
});

describe('HistoryStore [HC1][HC2]', () => {
  let harness: RouterTestingHarness;
  let http: HttpTestingController;
  let router: Router;

  beforeEach(async () => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([{ path: 'history', component: HistoryHost }]),
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
    harness = await RouterTestingHarness.create();
  });

  afterEach(() => {
    vi.useRealTimers();
    http.verify();
  });

  async function open(url: string): Promise<HistoryStore> {
    const host = await harness.navigateByUrl(url, HistoryHost);
    return host.store;
  }

  function nextRequest(): TestRequest {
    TestBed.tick();
    return http.expectOne((request) => request.url === '/api/history');
  }

  /** Waits until nothing is in flight; only call it when no request is pending. */
  async function settle(): Promise<void> {
    await TestBed.inject(ApplicationRef).whenStable();
  }

  /** Resolves when the next navigation ends; subscribe before triggering it. */
  function navigationEnd(): Promise<unknown> {
    return firstValueFrom(router.events.pipe(filter((event) => event instanceof NavigationEnd)));
  }

  it('asks the API for the view the URL describes', async () => {
    await open(
      '/history?from=2026-09-01&to=2026-09-07&suppliers=albatross-freight&sort=price&page=2',
    );

    const params = nextRequest().request.params;
    expect(params.get('startDate')).toBe(new Date(2026, 8, 1).toISOString());
    expect(params.get('endDate')).toBe(new Date(2026, 8, 8).toISOString());
    expect(params.getAll('suppliers')).toEqual(['albatross-freight']);
    expect([params.get('sortBy'), params.get('sortDirection')]).toEqual(['price', 'asc']);
    expect(params.get('page')).toBe('2');
  });

  it('shows the last seven days, newest first, when the URL has no parameters', async () => {
    const today = toLocalIsoDate(new Date());
    const store = await open('/history');

    const params = nextRequest().request.params;
    expect(store.query()).toMatchObject({ from: addDays(today, -6), to: today, sortBy: 'date' });
    expect([params.get('sortBy'), params.get('sortDirection')]).toEqual(['date', 'desc']);
    expect([params.get('page'), params.get('pageSize')]).toEqual(['1', '20']);
  });

  it('puts a new sort in the URL and starts again from the first page', async () => {
    const store = await open('/history?page=3');
    nextRequest().flush(page({ page: 3, totalCount: 60 }));

    const navigated = navigationEnd();
    store.sortBy('supplier');
    await navigated;

    expect(router.url).toBe('/history?sort=supplier');
    const params = nextRequest().request.params;
    expect([params.get('sortBy'), params.get('sortDirection'), params.get('page')]).toEqual([
      'supplier',
      'asc',
      '1',
    ]);
  });

  it('keeps the filters when paging', async () => {
    const store = await open('/history?suppliers=albatross-freight&failures=true');
    nextRequest().flush(page({ totalCount: 45 }));

    const navigated = navigationEnd();
    store.goToPage(2);
    await navigated;

    expect(router.url).toBe('/history?suppliers=albatross-freight&failures=true&page=2');
    expect(nextRequest().request.params.get('includeFailures')).toBe('true');
  });

  it('waits for typing to pause before filtering by location, then starts from page 1', async () => {
    const store = await open('/history?page=2');
    nextRequest().flush(page({ page: 2, totalCount: 30 }));
    vi.useFakeTimers();

    store.typeOrigin('Ha');
    await vi.advanceTimersByTimeAsync(TEXT_FILTER_DELAY_MS - 1);
    store.typeOrigin('Haifa');
    expect(store.originText()).toBe('Haifa');
    await vi.advanceTimersByTimeAsync(TEXT_FILTER_DELAY_MS - 1);
    expect(router.url).toBe('/history?page=2');

    const navigated = navigationEnd();
    await vi.advanceTimersByTimeAsync(1);
    vi.useRealTimers();
    await navigated;

    expect(router.url).toBe('/history?origin=Haifa');
    expect(nextRequest().request.params.get('origin')).toBe('Haifa');
  });

  it('keeps the location being typed when another filter changes before typing pauses', async () => {
    const store = await open('/history');
    nextRequest().flush(page());
    vi.useFakeTimers();

    store.typeOrigin('Rotterdam');
    const toggled = navigationEnd();
    store.setIncludeFailures(true);
    await vi.advanceTimersByTimeAsync(1);
    await toggled;

    expect(store.originText()).toBe('Rotterdam');
    nextRequest().flush(page());
    const typed = navigationEnd();
    await vi.advanceTimersByTimeAsync(TEXT_FILTER_DELAY_MS);
    await typed;
    expect(router.url).toBe('/history?origin=Rotterdam&failures=true');
    expect(nextRequest().request.params.get('origin')).toBe('Rotterdam');
  });

  it('follows the URL when it changes from outside, as back and forward do', async () => {
    const store = await open('/history?origin=haifa');
    nextRequest().flush(page());
    expect(store.originText()).toBe('haifa');

    await harness.navigateByUrl('/history');

    expect(store.originText()).toBe('');
    expect(nextRequest().request.params.has('origin')).toBe(false);
  });

  it('keeps the last page on screen while the next one loads', async () => {
    const store = await open('/history');
    nextRequest().flush(page({ totalCount: 45 }));
    await settle();
    // Rendering reads the page, which is when it's remembered.
    expect(store.page()?.page).toBe(1);

    const navigated = navigationEnd();
    store.goToPage(2);
    await navigated;
    const second = nextRequest();

    expect(store.loading()).toBe(true);
    expect(store.page()?.page).toBe(1);
    second.flush(page({ page: 2, totalCount: 45 }));
    await settle();
    expect(store.loading()).toBe(false);
    expect(store.page()?.page).toBe(2);
    expect(store.summary()).toMatchObject({ firstItem: 21, lastItem: 40, pageCount: 3 });
  });

  it('moves to the last page when the URL points past the end', async () => {
    await open('/history?page=9');
    const navigated = navigationEnd();
    nextRequest().flush(page({ items: [], page: 9, totalCount: 45 }));
    await navigated;

    expect(router.url).toBe('/history?page=3');
    expect(nextRequest().request.params.get('page')).toBe('3');
  });

  it('explains a failed load and loads again on retry', async () => {
    const store = await open('/history');
    nextRequest().flush('down', { status: 503, statusText: 'Service Unavailable' });
    await settle();

    expect(store.errorMessage()).toBe('The history could not be loaded. Please try again.');
    store.retry();
    nextRequest().flush(page());
    await settle();
    expect(store.errorMessage()).toBeNull();
  });

  it('shows the first field error of a rejected query', async () => {
    const store = await open('/history');
    nextRequest().flush(
      { title: 'One or more validation errors occurred.', errors: { endDate: ['Too early.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    await settle();

    expect(store.errorMessage()).toBe('Too early.');
  });

  it('keeps the date range in order when one end passes the other', async () => {
    const store = await open('/history?from=2026-09-01&to=2026-09-07');
    nextRequest().flush(page());

    let navigated = navigationEnd();
    store.setFrom('2026-09-10');
    await navigated;
    expect(store.query()).toMatchObject({ from: '2026-09-10', to: '2026-09-10' });
    nextRequest().flush(page());

    navigated = navigationEnd();
    store.setTo('2026-09-05');
    await navigated;
    expect(store.query()).toMatchObject({ from: '2026-09-05', to: '2026-09-05' });
    nextRequest().flush(page());

    // A cleared date field, or a year the browser's date spinner ran past, changes nothing.
    store.setFrom('');
    store.setTo('275759-09-28');
    expect(store.query()).toMatchObject({ from: '2026-09-05', to: '2026-09-05' });
    expect(router.url).toBe('/history?from=2026-09-05&to=2026-09-05');
  });
});
