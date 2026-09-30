import { expect, test } from './support/fixtures';
import { runSearch } from './support/api';
import { HistoryScreen, type HistoryRow } from './support/history-screen';
import { uniqueLocation } from './support/search-screen';

const byNumber = (a: number, b: number) => a - b;
const money = (text: string) => Number(/\$([\d,]+\.\d{2})/.exec(text)?.[1]?.replaceAll(',', ''));
const seconds = (text: string) => Number(/([\d.]+) s/.exec(text)?.[1]);
// ICU puts narrow no-break spaces into formatted times.
const instant = (text: string) => Date.parse(text.replace(/\s+/g, ' '));

/** Origin and destination, upper-cased the way the API compares them. */
function route(row: HistoryRow): string {
  const [origin = '', destination = ''] = (row.route.split('\n')[0] ?? '')
    .replace(/→\s*to/, '→')
    .split('→')
    .map((part) => part.trim().toUpperCase());
  return `${origin}\u0000${destination}`;
}

/** Each column and the value it sorts by, read back from the cells. */
const SORTS: readonly {
  readonly label: string;
  readonly key: (row: HistoryRow) => number | string;
}[] = [
  { label: 'Date', key: (row) => instant(row.date) },
  { label: 'Route', key: route },
  { label: 'Supplier', key: (row) => row.supplier },
  { label: 'Price', key: (row) => money(row.price) },
  { label: 'Response time', key: (row) => seconds(row.responseTime) },
];

function isOrdered(keys: readonly (number | string)[], direction: 'ascending' | 'descending') {
  return keys.every((key, index) => {
    const previous = keys[index - 1];
    if (previous === undefined) {
      return true;
    }
    return direction === 'ascending' ? previous <= key : previous >= key;
  });
}

test('the history filters, sorts by every column, pages and restores itself from the URL [HC1][HC2][H1][H2]', async ({
  page,
  request,
}) => {
  const prefix = uniqueLocation('Port');
  await Promise.all([
    runSearch(request, `${prefix} Haifa`, 'Rotterdam'),
    runSearch(request, `${prefix} Haifa`, 'Valencia'),
    runSearch(request, `${prefix} Ashdod`, 'Hamburg'),
  ]);

  const history = new HistoryScreen(page);
  await history.open(`/history?origin=${encodeURIComponent(prefix)}&size=10`);
  const total = Number(/of (\d+)/.exec(await history.showing())?.[1]);
  // Three searches of six suppliers; failures are left out until asked for.
  expect(total).toBeGreaterThanOrEqual(12);
  expect(total).toBeLessThanOrEqual(18);
  expect(await history.showing()).toBe(`Showing 1–10 of ${String(total)}`);
  for (const row of await history.readRows()) {
    expect(row.route).toContain(prefix);
  }

  // One supplier, several, then all again.
  await history.chooseSuppliers(['Albatross Freight']);
  expect(page.url()).toContain('suppliers=albatross-freight');
  expect(new Set((await history.readRows()).map((row) => row.supplier))).toEqual(
    new Set(['Albatross Freight']),
  );
  await history.chooseSuppliers(['Albatross Freight', 'Cobalt Harbor Lines']);
  expect(
    (await history.readRows()).every((row) =>
      ['Albatross Freight', 'Cobalt Harbor Lines'].includes(row.supplier),
    ),
  ).toBe(true);
  await history.chooseSuppliers([]);
  expect(await history.showing()).toBe(`Showing 1–10 of ${String(total)}`);

  // Every column sorts both ways, and says so with aria-sort. The view starts newest first, so
  // the first click on each column, Date included, sorts it ascending.
  await history.reloadsAfter(() => history.pageSize.selectOption('50'));
  for (const sort of SORTS) {
    for (const direction of ['ascending', 'descending'] as const) {
      await history.sortBy(sort.label);
      await expect(history.header(sort.label)).toHaveAttribute('aria-sort', direction);
      const keys = (await history.readRows()).map(sort.key);
      expect(keys, `${sort.label} ${direction}`).toHaveLength(total);
      expect(isOrdered(keys, direction), `${sort.label} ${direction}`).toBe(true);
    }
  }

  // Paging through, ten at a time.
  await history.reloadsAfter(() => history.pageSize.selectOption('10'));
  const pagination = history.pagination;
  await history.reloadsAfter(() => pagination.getByRole('button', { name: 'Next page' }).click());
  expect(await history.showing()).toBe(`Showing 11–${String(total)} of ${String(total)}`);
  expect(page.url()).toContain('page=2');
  await expect(pagination.getByRole('button', { name: 'Next page' })).toBeDisabled();
  await expect(pagination.getByRole('button', { name: 'Last page' })).toBeDisabled();
  await history.reloadsAfter(() => pagination.getByRole('button', { name: 'First page' }).click());
  expect(await history.showing()).toBe(`Showing 1–10 of ${String(total)}`);
  await history.reloadsAfter(() => pagination.getByRole('button', { name: 'Last page' }).click());
  await history.reloadsAfter(() =>
    pagination.getByRole('button', { name: 'Previous page' }).click(),
  );
  expect(await history.showing()).toBe(`Showing 1–10 of ${String(total)}`);

  // A filtered, sorted page survives a reload, and back and forward move between views.
  await history.chooseSuppliers(['Albatross Freight', 'Cobalt Harbor Lines']);
  await history.sortBy('Price');
  await history.sortBy('Price');
  const url = page.url();
  const before = await history.readRows();
  await history.reloadsAfter(() => page.reload());
  expect(page.url()).toBe(url);
  await expect(history.suppliers).toHaveText('Albatross Freight + 1 more');
  await expect(history.header('Price')).toHaveAttribute('aria-sort', 'descending');
  await expect(history.pageSize).toHaveValue('10');
  expect(await history.readRows()).toEqual(before);
  expect(before.map((row) => money(row.price))).toEqual(
    before
      .map((row) => money(row.price))
      .sort(byNumber)
      .reverse(),
  );

  await history.reloadsAfter(() => page.goBack());
  await expect(history.header('Price')).toHaveAttribute('aria-sort', 'ascending');
  await history.reloadsAfter(() => page.goForward());
  await expect(history.header('Price')).toHaveAttribute('aria-sort', 'descending');
});
