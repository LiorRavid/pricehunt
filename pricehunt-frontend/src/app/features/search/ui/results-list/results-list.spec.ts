import { TestBed, type ComponentFixture } from '@angular/core/testing';
import type { ResultRow } from '../../domain/result-row';
import { ResultsList } from './results-list';

const pending = (supplierId: string, supplierName: string): ResultRow => ({
  supplierId,
  supplierName,
  state: 'pending',
});
const quoted = (supplierId: string, supplierName: string, amount: number): ResultRow => ({
  supplierId,
  supplierName,
  state: 'quoted',
  price: { amount, currency: 'USD' },
  responseTimeMs: 1250,
});

describe('ResultsList', () => {
  let fixture: ComponentFixture<ResultsList>;

  beforeEach(() => {
    fixture = TestBed.createComponent(ResultsList);
  });

  async function render(rows: readonly ResultRow[]): Promise<HTMLLIElement[]> {
    fixture.componentRef.setInput('rows', rows);
    await fixture.whenStable();
    return Array.from((fixture.nativeElement as HTMLElement).querySelectorAll('li'));
  }

  it('renders one fixed slot per supplier, in the given order [CL2]', async () => {
    const items = await render([quoted('b', 'Bramble', 990.5), pending('a', 'Albatross')]);

    expect(items.map((item) => item.dataset['state'])).toEqual(['quoted', 'pending']);
    expect(items[0]?.textContent).toContain('$990.50');
    expect(items[0]?.textContent).toContain('Answered in 1.3 s');
    expect(items[1]?.textContent).toContain('Waiting for a quote');
    const list = (fixture.nativeElement as HTMLElement).querySelector('ol');
    expect(list?.getAttribute('aria-label')).toBe('Search results');
    expect(list?.style.height).toBe('calc(var(--spacing-result-row) * 2)');
  });

  it('places each row at its rank with a transform [CL3]', async () => {
    const items = await render([quoted('b', 'Bramble', 1), pending('a', 'Albatross')]);

    expect(items.map((item) => item.style.transform)).toEqual([
      'translateY(calc(var(--spacing-result-row) * 0))',
      'translateY(calc(var(--spacing-result-row) * 1))',
    ]);
  });

  it('keeps the same DOM nodes when rows are re-sorted [CL3]', async () => {
    const [albatross, bramble, copper] = await render([
      pending('a', 'Albatross'),
      pending('b', 'Bramble'),
      pending('c', 'Copper'),
    ]);

    const resorted = await render([
      quoted('c', 'Copper', 500),
      quoted('a', 'Albatross', 900),
      pending('b', 'Bramble'),
    ]);

    expect(resorted[0]).toBe(copper);
    expect(resorted[1]).toBe(albatross);
    expect(resorted[2]).toBe(bramble);
    expect(resorted.every((item) => item.isConnected)).toBe(true);
  });

  it('mutes failed and silent suppliers and shows no price for them', async () => {
    const items = await render([
      {
        supplierId: 'd',
        supplierName: 'Driftwood',
        state: 'failed',
        errorMessage: 'Driftwood is temporarily unavailable.',
        responseTimeMs: 900,
      },
      { supplierId: 'g', supplierName: 'Gullwing', state: 'noResponse' },
    ]);

    expect(items[0]?.className).toContain('text-slate-500');
    expect(items[0]?.textContent).toContain('Failed · Driftwood is temporarily unavailable.');
    expect(items[1]?.textContent).toContain('No response');
    expect(items.every((item) => item.textContent.includes('No price'))).toBe(true);
  });
});
