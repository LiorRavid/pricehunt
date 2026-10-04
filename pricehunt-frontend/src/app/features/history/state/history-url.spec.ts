import { convertToParamMap } from '@angular/router';
import type { HistoryQuery } from '../domain/history-query';
import { defaultHistoryQuery, readHistoryQuery, writeHistoryQuery } from './history-url';

const today = '2026-09-30';

const read = (params: Record<string, string | string[]>) =>
  readHistoryQuery(convertToParamMap(params), today);

const custom: HistoryQuery = {
  from: '2026-09-01',
  to: '2026-09-15',
  suppliers: ['albatross-freight', 'cobalt-harbor-lines'],
  origin: 'Haifa ',
  destination: 'rotter',
  includeFailures: true,
  sortBy: 'price',
  sortDirection: 'desc',
  page: 3,
  pageSize: 50,
};

describe('history URL [HC1][HC2]', () => {
  it('shows the last seven days of every supplier, newest first, by default', () => {
    expect(read({})).toEqual({
      from: '2026-09-24',
      to: '2026-09-30',
      suppliers: [],
      origin: '',
      destination: '',
      includeFailures: false,
      sortBy: 'date',
      sortDirection: 'desc',
      page: 1,
      pageSize: 20,
    });
    expect(defaultHistoryQuery(today)).toEqual(read({}));
  });

  it('reads every parameter', () => {
    expect(
      read({
        from: '2026-09-01',
        to: '2026-09-15',
        suppliers: ['albatross-freight', 'cobalt-harbor-lines'],
        origin: 'Haifa ',
        destination: 'rotter',
        failures: 'true',
        sort: 'price',
        dir: 'desc',
        page: '3',
        size: '50',
      }),
    ).toEqual(custom);
  });

  it('writes only what differs from the default, so links stay short', () => {
    expect(writeHistoryQuery(defaultHistoryQuery(today), today)).toEqual({});
    expect(
      writeHistoryQuery(
        { ...defaultHistoryQuery(today), sortBy: 'price', sortDirection: 'asc' },
        today,
      ),
    ).toEqual({ sort: 'price' });
    expect(
      writeHistoryQuery(
        { ...defaultHistoryQuery(today), sortBy: 'price', sortDirection: 'desc' },
        today,
      ),
    ).toEqual({ sort: 'price', dir: 'desc' });
  });

  it('writes both dates once either one changes', () => {
    expect(writeHistoryQuery({ ...defaultHistoryQuery(today), from: '2026-09-20' }, today)).toEqual(
      { from: '2026-09-20', to: '2026-09-30' },
    );
  });

  it('round-trips a view through the URL', () => {
    const params = writeHistoryQuery(custom, today);

    expect(params).toEqual({
      from: '2026-09-01',
      to: '2026-09-15',
      suppliers: ['albatross-freight', 'cobalt-harbor-lines'],
      origin: 'Haifa ',
      destination: 'rotter',
      failures: 'true',
      sort: 'price',
      dir: 'desc',
      page: '3',
      size: '50',
    });
    expect(read(params)).toEqual(custom);
  });

  it('falls back to the default for anything invalid', () => {
    const query = read({
      from: '2026-02-31',
      to: 'yesterday',
      suppliers: ['albatross-freight', 'Not An Id', 'albatross-freight'],
      failures: 'yes',
      sort: 'cost',
      dir: 'up',
      page: '0',
      size: '7',
    });

    expect(query).toEqual({
      ...defaultHistoryQuery(today),
      suppliers: ['albatross-freight'],
    });
    expect(read({ page: '2.5' }).page).toBe(1);
    expect(read({ page: 'abc' }).page).toBe(1);
  });

  it('keeps every value within what the API accepts', () => {
    expect(
      read({
        from: '2026-09-20',
        to: '9999-12-31',
        page: '3000000000',
        suppliers: ['a'.repeat(65), 'albatross-freight'],
      }),
    ).toMatchObject({ from: '2026-09-20', to: today, page: 1, suppliers: ['albatross-freight'] });
    // A link shared from a time zone that's already in tomorrow.
    expect(read({ from: '2026-10-01', to: '2026-10-01' })).toMatchObject({
      from: today,
      to: today,
    });
    expect(read({ page: '2147483647' }).page).toBe(2_147_483_647);
  });

  it('sorts in the column default direction when the URL has none', () => {
    expect(read({ sort: 'price' }).sortDirection).toBe('asc');
    expect(read({ sort: 'date' }).sortDirection).toBe('desc');
  });

  it('puts reversed dates back in order and caps long location text', () => {
    const query = read({ from: '2026-09-15', to: '2026-09-01', origin: 'x'.repeat(150) });

    expect([query.from, query.to]).toEqual(['2026-09-01', '2026-09-15']);
    expect(query.origin).toHaveLength(100);
  });
});
