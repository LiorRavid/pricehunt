import type { SearchEvent } from '../domain/search-event';
import { searchReducer, type SearchAction } from './search-reducer';
import { initialSearchState, type SearchState } from './search-state';
import { selectBadge, selectProgress } from './search-view';

const suppliers = [
  { id: 'albatross', name: 'Albatross' },
  { id: 'bramble', name: 'Bramble' },
  { id: 'gullwing', name: 'Gullwing' },
];

const started = (searchId = 's1'): SearchEvent => ({
  type: 'started',
  searchId,
  suppliers,
  maxDurationMs: 6000,
});
const quote = (supplierId: string, amount: number, searchId = 's1'): SearchEvent => ({
  type: 'quote',
  searchId,
  supplierId,
  price: { amount, currency: 'USD' },
  responseTimeMs: 1000,
});

function reduce(...actions: SearchAction[]): SearchState {
  return actions.reduce(searchReducer, initialSearchState);
}

const request = (attempt = 1): SearchAction => ({ type: 'searchRequested', attempt, suppliers });
const receive = (event: SearchEvent, attempt = 1): SearchAction => ({
  type: 'eventReceived',
  attempt,
  event,
});

describe('searchReducer', () => {
  it('starts with one pending row per selected supplier [CL1]', () => {
    const state = reduce(request());

    expect(state.phase).toBe('searching');
    expect(state.rows.map((row) => [row.supplierName, row.state])).toEqual([
      ['Albatross', 'pending'],
      ['Bramble', 'pending'],
      ['Gullwing', 'pending'],
    ]);
  });

  it('records the search id and the time budget when the search starts', () => {
    const state = reduce(request(), receive(started()));

    expect(state.searchId).toBe('s1');
    expect(state.maxDurationMs).toBe(6000);
  });

  it('fills a supplier row with its quote [CL2]', () => {
    const state = reduce(request(), receive(started()), receive(quote('bramble', 990)));

    expect(state.rows[1]).toEqual({
      supplierId: 'bramble',
      supplierName: 'Bramble',
      state: 'quoted',
      price: { amount: 990, currency: 'USD' },
      responseTimeMs: 1000,
    });
  });

  it('records a supplier failure and counts it as responded [CL4]', () => {
    const state = reduce(
      request(),
      receive(started()),
      receive({
        type: 'failure',
        searchId: 's1',
        supplierId: 'albatross',
        errorMessage: 'Down.',
        responseTimeMs: 800,
      }),
      receive(quote('bramble', 990)),
    );

    expect(state.rows[0]?.state).toBe('failed');
    expect(selectProgress(state)).toEqual({
      responded: 2,
      failed: 1,
      total: 3,
      pending: [{ id: 'gullwing', name: 'Gullwing' }],
    });
  });

  it('ignores events from another search id [CL5]', () => {
    const state = reduce(
      request(),
      receive(started()),
      receive(quote('bramble', 990, 'old-search')),
    );

    expect(state.rows.every((row) => row.state === 'pending')).toBe(true);
  });

  it('ignores events tagged with an older attempt [CL5]', () => {
    const state = reduce(
      request(1),
      receive(started()),
      request(2),
      receive(started('s1'), 1),
      receive(quote('bramble', 990), 1),
    );

    expect(state.attempt).toBe(2);
    expect(state.searchId).toBeNull();
    expect(state.rows.every((row) => row.state === 'pending')).toBe(true);
  });

  it('ignores a quote before the search has started, and a second start', () => {
    const early = reduce(request(), receive(quote('bramble', 990)));
    const twice = reduce(request(), receive(started('s1')), receive(started('s2')));

    expect(early.rows.every((row) => row.state === 'pending')).toBe(true);
    expect(twice.searchId).toBe('s1');
  });

  it('ignores a second answer from the same supplier', () => {
    const state = reduce(
      request(),
      receive(started()),
      receive(quote('bramble', 990)),
      receive(quote('bramble', 1)),
    );

    expect(state.rows[1]).toMatchObject({ state: 'quoted', price: { amount: 990 } });
  });

  it('ends completed when the server says so [CL4]', () => {
    const state = reduce(
      request(),
      receive(started()),
      receive(quote('albatross', 1)),
      receive(quote('bramble', 2)),
      receive(quote('gullwing', 3)),
      receive({
        type: 'completed',
        searchId: 's1',
        outcome: 'Completed',
        noResponseSupplierIds: [],
      }),
    );

    expect(state.phase).toBe('completed');
    expect(selectBadge(state)).toEqual({
      kind: 'completed',
      title: 'Completed',
      detail: '3 of 3 suppliers responded.',
    });
  });

  it('ends timed out and marks the silent suppliers [SV3][CL4]', () => {
    const state = reduce(
      request(),
      receive(started()),
      receive(quote('bramble', 990)),
      receive({
        type: 'completed',
        searchId: 's1',
        outcome: 'TimedOut',
        noResponseSupplierIds: ['albatross', 'gullwing'],
      }),
    );

    expect(state.phase).toBe('timedOut');
    expect(state.rows.map((row) => row.state)).toEqual(['noResponse', 'quoted', 'noResponse']);
    expect(selectBadge(state)).toEqual({
      kind: 'timedOut',
      title: 'Timed out',
      detail: 'No response from Albatross, Gullwing.',
    });
  });

  it('ends in error when the server faults', () => {
    const state = reduce(
      request(),
      receive(started()),
      receive({
        type: 'completed',
        searchId: 's1',
        outcome: 'Faulted',
        noResponseSupplierIds: ['albatross', 'bramble', 'gullwing'],
      }),
    );

    expect(state.phase).toBe('error');
    expect(selectBadge(state)?.detail).toBe('The search failed on the server.');
  });

  it('ignores everything after the terminal event [CL5]', () => {
    const ended = reduce(
      request(),
      receive(started()),
      receive({
        type: 'completed',
        searchId: 's1',
        outcome: 'TimedOut',
        noResponseSupplierIds: ['albatross', 'bramble', 'gullwing'],
      }),
    );

    expect(searchReducer(ended, receive(quote('bramble', 990)))).toBe(ended);
    expect(searchReducer(ended, { type: 'searchCancelled', attempt: 1 })).toBe(ended);
    expect(searchReducer(ended, { type: 'streamFailed', attempt: 1, message: 'Lost.' })).toBe(
      ended,
    );
  });

  it('ends cancelled on request and stops waiting for the pending suppliers', () => {
    const state = reduce(request(), receive(started()), receive(quote('bramble', 990)), {
      type: 'searchCancelled',
      attempt: 1,
    });

    expect(state.phase).toBe('cancelled');
    expect(state.rows.map((row) => row.state)).toEqual(['noResponse', 'quoted', 'noResponse']);
    expect(selectBadge(state)).toEqual({
      kind: 'cancelled',
      title: 'Cancelled',
      detail: 'The search was stopped.',
    });
  });

  it('ends in error when the stream fails', () => {
    const state = reduce(request(), {
      type: 'streamFailed',
      attempt: 1,
      message: 'The connection to the server was lost.',
    });

    expect(state.phase).toBe('error');
    expect(selectBadge(state)).toEqual({
      kind: 'error',
      title: 'Error',
      detail: 'The connection to the server was lost.',
    });
  });

  it('shows no badge while idle or searching', () => {
    expect(selectBadge(initialSearchState)).toBeNull();
    expect(selectBadge(reduce(request()))).toBeNull();
  });

  it('starts over with a new search', () => {
    const state = reduce(
      request(1),
      receive(started()),
      receive(quote('bramble', 990)),
      request(2),
    );

    expect(state).toMatchObject({
      phase: 'searching',
      attempt: 2,
      searchId: null,
      maxDurationMs: null,
      errorMessage: null,
    });
    expect(state.rows.every((row) => row.state === 'pending')).toBe(true);
  });
});
