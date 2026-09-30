import { TestBed } from '@angular/core/testing';
import { firstValueFrom, Subject, toArray } from 'rxjs';
import { SseClient } from '../../../core/sse/sse-client';
import type { SseMessage } from '../../../core/sse/sse-parser';
import { SearchApi } from './search-api';
import { toSearchEvent } from './search-event-mapper';

describe('SearchApi', () => {
  it('posts trimmed criteria, ends at search-completed and maps each event', async () => {
    const messages = new Subject<SseMessage>();
    const post = vi.fn(() => messages.asObservable());
    TestBed.configureTestingModule({ providers: [{ provide: SseClient, useValue: { post } }] });
    const events = firstValueFrom(
      TestBed.inject(SearchApi)
        .stream({
          origin: ' Haifa ',
          destination: 'Rotterdam ',
          fromDate: '2026-10-01',
          toDate: '2026-10-08',
          supplierIds: ['a'],
        })
        .pipe(toArray()),
    );

    messages.next({
      event: 'search-started',
      data: '{"searchId":"s1","suppliers":[{"id":"a","name":"A"}],"maxDurationMs":6000}',
      id: '1',
    });
    messages.next({ event: 'keep-alive', data: '{}', id: '2' });
    messages.complete();

    expect(await events).toEqual([
      { type: 'started', searchId: 's1', suppliers: [{ id: 'a', name: 'A' }], maxDurationMs: 6000 },
    ]);
    const [url, body, isTerminal] = post.mock.calls[0] as unknown as [
      string,
      unknown,
      (message: SseMessage) => boolean,
    ];
    expect(url).toBe('/api/searches');
    expect(body).toEqual({
      origin: 'Haifa',
      destination: 'Rotterdam',
      fromDate: '2026-10-01',
      toDate: '2026-10-08',
      supplierIds: ['a'],
    });
    expect(isTerminal({ event: 'search-completed', data: '{}', id: '' })).toBe(true);
    expect(isTerminal({ event: 'quote-received', data: '{}', id: '' })).toBe(false);
  });
});

describe('toSearchEvent', () => {
  it('maps a quote', () => {
    const data =
      '{"searchId":"s1","supplierId":"a","price":{"amount":1234.56,"currency":"USD"},"responseTimeMs":1200,"receivedAt":"2026-09-30T10:00:01Z"}';

    expect(toSearchEvent({ event: 'quote-received', data, id: '2' })).toEqual({
      type: 'quote',
      searchId: 's1',
      supplierId: 'a',
      price: { amount: 1234.56, currency: 'USD' },
      responseTimeMs: 1200,
    });
  });

  it('maps a failure', () => {
    const data =
      '{"searchId":"s1","supplierId":"d","errorCode":"supplier_unavailable","errorMessage":"Down.","responseTimeMs":900,"receivedAt":"2026-09-30T10:00:01Z"}';

    expect(toSearchEvent({ event: 'supplier-failed', data, id: '3' })).toEqual({
      type: 'failure',
      searchId: 's1',
      supplierId: 'd',
      errorMessage: 'Down.',
      responseTimeMs: 900,
    });
  });

  it('maps the terminal event', () => {
    const data =
      '{"searchId":"s1","status":"TimedOut","completedAt":"2026-09-30T10:00:06Z","respondedCount":6,"succeededCount":6,"failedCount":0,"noResponseSupplierIds":["g"]}';

    expect(toSearchEvent({ event: 'search-completed', data, id: '9' })).toEqual({
      type: 'completed',
      searchId: 's1',
      outcome: 'TimedOut',
      noResponseSupplierIds: ['g'],
    });
  });

  it('ignores unknown event types', () => {
    expect(toSearchEvent({ event: 'message', data: 'x', id: '' })).toBeNull();
  });
});
