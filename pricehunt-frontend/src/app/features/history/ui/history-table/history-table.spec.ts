import { TestBed, type ComponentFixture } from '@angular/core/testing';
import type { HistoryItem } from '../../domain/history-item';
import type { HistorySortField } from '../../domain/history-query';
import { HistoryTable } from './history-table';

const quote: HistoryItem = {
  id: 'r1',
  searchId: 's1',
  receivedAt: new Date(2026, 8, 30, 17, 45),
  origin: 'Haifa',
  destination: 'Rotterdam',
  shipDateFrom: '2026-09-30',
  shipDateTo: '2026-10-07',
  supplierId: 'albatross-freight',
  supplierName: 'Albatross Freight',
  outcome: 'Succeeded',
  price: { amount: 1684.43, currency: 'USD' },
  responseTimeMs: 960,
};

const timedOut: HistoryItem = {
  ...quote,
  id: 'r2',
  supplierId: 'gullwing-transport',
  supplierName: 'Gullwing Transport',
  outcome: 'TimedOut',
  price: null,
  responseTimeMs: 6000,
};

// ICU separates some date parts with narrow no-break spaces.
const plain = (text: string | null | undefined) => (text ?? '').replace(/\s+/g, ' ').trim();

/** The text a screen reader gets: decorative parts dropped, visually hidden parts kept. */
function readable(node: Element): string {
  const copy = node.cloneNode(true) as Element;
  copy.querySelectorAll('[aria-hidden="true"]').forEach((hidden) => {
    hidden.remove();
  });
  copy.querySelectorAll('span, p').forEach((part) => {
    part.append(' ');
  });
  return plain(copy.textContent);
}

describe('HistoryTable [HC2]', () => {
  let fixture: ComponentFixture<HistoryTable>;
  let element: HTMLElement;

  async function render(inputs: {
    items: readonly HistoryItem[] | undefined;
    loading?: boolean;
    errorMessage?: string | null;
    sortBy?: HistorySortField;
  }) {
    fixture = TestBed.createComponent(HistoryTable);
    fixture.componentRef.setInput('items', inputs.items);
    fixture.componentRef.setInput('loading', inputs.loading ?? false);
    fixture.componentRef.setInput('errorMessage', inputs.errorMessage ?? null);
    fixture.componentRef.setInput('sortBy', inputs.sortBy ?? 'date');
    fixture.componentRef.setInput('sortDirection', 'desc');
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  const rows = () => Array.from(element.querySelectorAll('tbody tr'));
  const cells = (row: Element | undefined) =>
    Array.from(row?.querySelectorAll('td') ?? [], (cell) => readable(cell));

  it('shows each quote with local time, route, ship dates, price and response time', async () => {
    await render({ items: [quote] });

    expect(cells(rows()[0])).toEqual([
      'Sep 30, 2026, 5:45 PM',
      'Haifa to Rotterdam Ships Sep 30 – Oct 7, 2026',
      'Albatross Freight',
      '$1,684.43',
      '1.0 s',
    ]);
  });

  it('marks a response without a price with its outcome and a dash', async () => {
    await render({ items: [timedOut] });

    const [row] = rows();
    expect(cells(row)[3]).toBe('Timed out No price');
    expect(row?.className).toContain('text-slate-500');
  });

  it('marks the sorted column with aria-sort and emits the column to sort by', async () => {
    const sorted: HistorySortField[] = [];
    await render({ items: [quote], sortBy: 'price' });
    fixture.componentInstance.sortChange.subscribe((field) => sorted.push(field));

    const headers = Array.from(element.querySelectorAll('th'));
    expect(headers.map((header) => header.getAttribute('aria-sort'))).toEqual([
      'none',
      'none',
      'none',
      'descending',
      'none',
    ]);
    headers.forEach((header) => header.querySelector('button')?.click());
    expect(sorted).toEqual(['date', 'route', 'supplier', 'price', 'responseTime']);
  });

  it('shows skeleton rows until the first page arrives', async () => {
    await render({ items: undefined, loading: true });

    expect(rows()).toHaveLength(5);
    expect(element.querySelector('[aria-busy="true"]')).not.toBeNull();
    expect(element.querySelector('[role="status"]')?.textContent).toContain(
      'Loading the price history',
    );
  });

  it('keeps the previous rows, dimmed, while the next page loads', async () => {
    await render({ items: [quote], loading: true });

    expect(cells(rows()[0])[2]).toBe('Albatross Freight');
    expect(element.querySelector('tbody')?.className).toContain('opacity-60');
  });

  it('explains when nothing matches', async () => {
    await render({ items: [] });

    expect(
      Array.from(element.querySelectorAll('tbody p'), (paragraph) => plain(paragraph.textContent)),
    ).toEqual(['No quotes match these filters.', 'Try a wider date range, or run a search first.']);
  });

  it('shows an error with a retry button instead of the table', async () => {
    let retried = 0;
    await render({ items: [quote], errorMessage: 'The history could not be loaded.' });
    fixture.componentInstance.retry.subscribe(() => retried++);

    expect(element.querySelector('table')).toBeNull();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'The history could not be loaded.',
    );
    element.querySelector('button')?.click();
    expect(retried).toBe(1);
  });
});
