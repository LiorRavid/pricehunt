import type { HistoryQuery } from '../domain/history-query';
import { toHistoryParams } from './history-params';

const query: HistoryQuery = {
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
};

describe('toHistoryParams [H1][H2]', () => {
  it('turns local days into a half-open range of UTC instants', () => {
    const params = toHistoryParams(query);

    expect(params.get('startDate')).toBe(new Date(2026, 8, 24).toISOString());
    expect(params.get('endDate')).toBe(new Date(2026, 9, 1).toISOString());
  });

  it('ends a single day at the next local midnight, across month and year ends', () => {
    const oneDay = toHistoryParams({ ...query, from: '2026-12-31', to: '2026-12-31' });

    expect(oneDay.get('startDate')).toBe(new Date(2026, 11, 31).toISOString());
    expect(oneDay.get('endDate')).toBe(new Date(2027, 0, 1).toISOString());
  });

  it('always sends the sort and the page, and only the filters in use', () => {
    const params = toHistoryParams({ ...query, sortBy: 'price', sortDirection: 'asc', page: 3 });

    expect(params.keys()).toEqual([
      'startDate',
      'endDate',
      'sortBy',
      'sortDirection',
      'page',
      'pageSize',
    ]);
    expect([params.get('sortBy'), params.get('sortDirection')]).toEqual(['price', 'asc']);
    expect([params.get('page'), params.get('pageSize')]).toEqual(['3', '20']);
  });

  it('repeats the suppliers and trims the location filters', () => {
    const params = toHistoryParams({
      ...query,
      suppliers: ['albatross-freight', 'cobalt-harbor-lines'],
      origin: '  haifa ',
      destination: ' ',
    });

    expect(params.getAll('suppliers')).toEqual(['albatross-freight', 'cobalt-harbor-lines']);
    expect(params.get('origin')).toBe('haifa');
    expect(params.has('destination')).toBe(false);
  });

  it('asks for failed and missing responses only when they are included', () => {
    expect(toHistoryParams(query).has('includeFailures')).toBe(false);
    expect(toHistoryParams({ ...query, includeFailures: true }).get('includeFailures')).toBe(
      'true',
    );
  });
});
