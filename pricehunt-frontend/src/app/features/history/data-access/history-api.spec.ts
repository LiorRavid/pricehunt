import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ApplicationRef, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import type { HistoryQuery } from '../domain/history-query';
import { HistoryApi } from './history-api';
import type { HistoryPageDto } from './history-dtos';

const query: HistoryQuery = {
  from: '2026-09-24',
  to: '2026-09-30',
  suppliers: [],
  origin: '',
  destination: '',
  includeFailures: true,
  sortBy: 'date',
  sortDirection: 'desc',
  page: 1,
  pageSize: 20,
};

const body: HistoryPageDto = {
  items: [
    {
      id: 'r1',
      searchId: 's1',
      receivedAt: '2026-09-30T14:45:05.653Z',
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
    {
      id: 'r2',
      searchId: 's1',
      receivedAt: '2026-09-30T14:45:10.479Z',
      origin: 'Haifa',
      destination: 'Rotterdam',
      shipDateFrom: '2026-09-30',
      shipDateTo: '2026-10-07',
      supplierId: 'gullwing-transport',
      supplierName: 'Gullwing Transport',
      outcome: 'TimedOut',
      price: null,
      responseTimeMs: 6000,
      errorCode: null,
    },
  ],
  page: 1,
  pageSize: 20,
  totalCount: 2,
};

describe('HistoryApi [H1][HC1]', () => {
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  function load(initial: HistoryQuery) {
    const current = signal(initial);
    const history = TestBed.runInInjectionContext(() =>
      TestBed.inject(HistoryApi).load(current.asReadonly()),
    );
    return { current, history };
  }

  it('requests the page the query describes and maps the response', async () => {
    const { history } = load(query);
    TestBed.tick();

    const request = http.expectOne((candidate) => candidate.url === '/api/history');
    expect(request.request.params.get('includeFailures')).toBe('true');
    request.flush(body);
    await TestBed.inject(ApplicationRef).whenStable();

    const page = history.value();
    expect(page?.totalCount).toBe(2);
    expect(page?.items[0]?.receivedAt).toEqual(new Date(Date.UTC(2026, 8, 30, 14, 45, 5, 653)));
    expect(page?.items[0]?.price).toEqual({ amount: 1684.43, currency: 'USD' });
    expect(page?.items[1]).toMatchObject({ outcome: 'TimedOut', price: null });
  });

  it('cancels a request the next query has superseded', async () => {
    const { current, history } = load(query);
    TestBed.tick();
    const first = http.expectOne((candidate) => candidate.url === '/api/history');

    current.set({ ...query, page: 2 });
    TestBed.tick();
    const second = http.expectOne((candidate) => candidate.url === '/api/history');

    expect(first.cancelled).toBe(true);
    expect(second.request.params.get('page')).toBe('2');
    second.flush({ ...body, page: 2 });
    await TestBed.inject(ApplicationRef).whenStable();
    expect(history.value()?.page).toBe(2);
  });
});
