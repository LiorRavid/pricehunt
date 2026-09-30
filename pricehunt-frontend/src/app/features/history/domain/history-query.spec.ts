import { sameHistoryQuery, toggleSort, type HistoryQuery } from './history-query';

const query: HistoryQuery = {
  from: '2026-09-24',
  to: '2026-09-30',
  suppliers: [],
  origin: '',
  destination: '',
  includeFailures: false,
  sortBy: 'date',
  sortDirection: 'desc',
  page: 3,
  pageSize: 20,
};

describe('toggleSort [HC2]', () => {
  it('flips the direction of the current sort column', () => {
    expect(toggleSort(query, 'date')).toMatchObject({ sortBy: 'date', sortDirection: 'asc' });
    expect(toggleSort({ ...query, sortDirection: 'asc' }, 'date').sortDirection).toBe('desc');
  });

  it('sorts a new column ascending, and dates newest first', () => {
    expect(toggleSort(query, 'price')).toMatchObject({ sortBy: 'price', sortDirection: 'asc' });
    expect(toggleSort({ ...query, sortBy: 'price' }, 'date')).toMatchObject({
      sortBy: 'date',
      sortDirection: 'desc',
    });
  });

  it('goes back to the first page and keeps the filters', () => {
    const sorted = toggleSort({ ...query, suppliers: ['albatross-freight'] }, 'supplier');

    expect(sorted.page).toBe(1);
    expect(sorted.suppliers).toEqual(['albatross-freight']);
  });
});

describe('sameHistoryQuery', () => {
  it('compares every field, including the supplier list', () => {
    expect(sameHistoryQuery(query, { ...query, suppliers: [] })).toBe(true);
    expect(sameHistoryQuery(query, { ...query, page: 4 })).toBe(false);
    expect(
      sameHistoryQuery(
        { ...query, suppliers: ['albatross-freight', 'cobalt-harbor-lines'] },
        { ...query, suppliers: ['albatross-freight', 'driftwood-shipping'] },
      ),
    ).toBe(false);
    expect(sameHistoryQuery(query, { ...query, suppliers: ['albatross-freight'] })).toBe(false);
  });
});
