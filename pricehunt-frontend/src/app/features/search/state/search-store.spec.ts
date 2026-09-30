import { TestBed } from '@angular/core/testing';
import { finalize, Subject } from 'rxjs';
import { HttpProblemError } from '../../../core/http/problem-details';
import { SseConnectionError } from '../../../core/sse/sse-connection-error';
import { SearchApi } from '../data-access/search-api';
import type { SearchCriteria } from '../domain/search-criteria';
import type { SearchEvent } from '../domain/search-event';
import { describeSearchError } from './search-error';
import { SearchStore } from './search-store';

const criteria: SearchCriteria = {
  origin: 'Haifa',
  destination: 'Rotterdam',
  fromDate: '2026-10-01',
  toDate: '2026-10-08',
  supplierIds: ['albatross', 'bramble'],
};
const suppliers = [
  { id: 'albatross', name: 'Albatross' },
  { id: 'bramble', name: 'Bramble' },
];

/** A search API whose streams the test drives; it records when each stream is torn down. */
class FakeSearchApi {
  readonly streams: { events: Subject<SearchEvent>; unsubscribed: boolean }[] = [];

  stream() {
    const stream = { events: new Subject<SearchEvent>(), unsubscribed: false };
    this.streams.push(stream);
    return stream.events.pipe(finalize(() => (stream.unsubscribed = true)));
  }
}

describe('SearchStore', () => {
  let api: FakeSearchApi;
  let store: SearchStore;

  beforeEach(() => {
    api = new FakeSearchApi();
    TestBed.configureTestingModule({
      providers: [SearchStore, { provide: SearchApi, useValue: api }],
    });
    store = TestBed.inject(SearchStore);
  });

  it('streams a search into sorted rows [CL1][CL2]', () => {
    store.search(criteria, suppliers);
    const events = api.streams[0]?.events;
    events?.next({ type: 'started', searchId: 's1', suppliers, maxDurationMs: 6000 });
    events?.next({
      type: 'quote',
      searchId: 's1',
      supplierId: 'bramble',
      price: { amount: 900, currency: 'USD' },
      responseTimeMs: 800,
    });

    expect(store.searching()).toBe(true);
    expect(store.maxDurationMs()).toBe(6000);
    expect(store.rows().map((row) => [row.supplierName, row.state])).toEqual([
      ['Bramble', 'quoted'],
      ['Albatross', 'pending'],
    ]);
  });

  it('cancels the previous stream when a new search starts, and ignores its late events [CL5]', () => {
    store.search(criteria, suppliers);
    const first = api.streams[0];
    first?.events.next({ type: 'started', searchId: 'old', suppliers, maxDurationMs: 6000 });

    store.search(criteria, suppliers);
    first?.events.next({
      type: 'quote',
      searchId: 'old',
      supplierId: 'bramble',
      price: { amount: 1, currency: 'USD' },
      responseTimeMs: 1,
    });

    expect(first?.unsubscribed).toBe(true);
    expect(api.streams).toHaveLength(2);
    expect(store.attempt()).toBe(2);
    expect(store.rows().every((row) => row.state === 'pending')).toBe(true);
  });

  it('cancels the running search on request', () => {
    store.search(criteria, suppliers);

    store.cancel();

    expect(api.streams[0]?.unsubscribed).toBe(true);
    expect(store.phase()).toBe('cancelled');
    expect(store.badge()?.title).toBe('Cancelled');
  });

  it('does nothing when cancelling while idle', () => {
    store.cancel();

    expect(store.phase()).toBe('idle');
  });

  it('turns a stream failure into an error state', () => {
    store.search(criteria, suppliers);

    api.streams[0]?.events.error(new SseConnectionError());

    expect(store.phase()).toBe('error');
    expect(store.badge()?.detail).toBe('The connection to the server was lost.');
  });

  it('ends the stream when the store is destroyed', () => {
    store.search(criteria, suppliers);

    TestBed.resetTestingModule();

    expect(api.streams[0]?.unsubscribed).toBe(true);
  });
});

describe('describeSearchError', () => {
  it('shows the first field error of a rejected search', () => {
    const error = new HttpProblemError(400, {
      title: 'Invalid.',
      errors: { origin: ['A location is required.'] },
    });

    expect(describeSearchError(error)).toBe('A location is required.');
  });

  it('falls back to the problem title, then a generic message', () => {
    expect(describeSearchError(new HttpProblemError(409, { title: 'Conflict.' }))).toBe(
      'Conflict.',
    );
    expect(describeSearchError(new HttpProblemError(400, {}))).toBe('The search was rejected.');
  });

  it('hides server details behind a friendly message', () => {
    expect(
      describeSearchError(new HttpProblemError(500, { title: 'An unexpected error occurred.' })),
    ).toBe('The server could not run the search. Please try again.');
  });

  it('explains connection problems and anything unexpected', () => {
    expect(describeSearchError(new SseConnectionError('The server could not be reached.'))).toBe(
      'The server could not be reached.',
    );
    expect(describeSearchError(new Error('boom'))).toBe('Something went wrong. Please try again.');
  });
});
