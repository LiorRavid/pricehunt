import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { summarizePage } from '../../domain/page-summary';
import { HistoryPagination } from './history-pagination';

describe('HistoryPagination [HC2]', () => {
  let fixture: ComponentFixture<HistoryPagination>;
  let element: HTMLElement;
  let pages: number[];
  let sizes: number[];

  async function render(page: number, pageSize: number, totalCount: number) {
    fixture = TestBed.createComponent(HistoryPagination);
    fixture.componentRef.setInput('summary', summarizePage(page, pageSize, totalCount));
    fixture.componentRef.setInput('pageSize', pageSize);
    pages = [];
    sizes = [];
    fixture.componentInstance.pageChange.subscribe((next) => pages.push(next));
    fixture.componentInstance.pageSizeChange.subscribe((next) => sizes.push(next));
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  }

  function button(label: string): HTMLButtonElement {
    const found = element.querySelector(`button[aria-label="${label}"]`);
    if (!(found instanceof HTMLButtonElement)) {
      throw new Error(`No button is labelled "${label}".`);
    }
    return found;
  }

  const text = () => element.textContent.replace(/\s+/g, ' ');

  it('shows which rows are on screen and which page this is', async () => {
    await render(2, 20, 312);

    expect(text()).toContain('Showing 21–40 of 312');
    expect(text()).toContain('Page 2 of 16');
    expect(element.querySelector('p')?.getAttribute('aria-live')).toBe('polite');
  });

  it('moves to the first, previous, next and last page', async () => {
    await render(2, 20, 312);

    for (const label of ['First page', 'Previous page', 'Next page', 'Last page']) {
      button(label).click();
    }

    expect(pages).toEqual([1, 1, 3, 16]);
  });

  it('can only go forward from the first page and back from the last', async () => {
    await render(1, 20, 45);
    expect([button('First page').disabled, button('Previous page').disabled]).toEqual([true, true]);
    expect([button('Next page').disabled, button('Last page').disabled]).toEqual([false, false]);

    await render(3, 20, 45);
    expect([button('Next page').disabled, button('Last page').disabled]).toEqual([true, true]);
    expect(text()).toContain('Showing 41–45 of 45');
  });

  it('says so when nothing matches, with every page button off', async () => {
    await render(1, 20, 0);

    expect(text()).toContain('No results');
    expect(text()).toContain('Page 1 of 1');
    expect(Array.from(element.querySelectorAll('button')).every((b) => b.disabled)).toBe(true);
  });

  it('offers page sizes up to 100 and emits the chosen one', async () => {
    await render(1, 20, 312);
    const select = element.querySelector('select');

    expect(Array.from(select?.options ?? [], (option) => option.text)).toEqual([
      '10',
      '20',
      '50',
      '100',
    ]);
    expect(select?.value).toBe('20');
    if (select) {
      select.value = '50';
      select.dispatchEvent(new Event('change'));
    }
    expect(sizes).toEqual([50]);
  });
});
