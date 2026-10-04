import { TestBed, type ComponentFixture } from '@angular/core/testing';
import type { HistoryQuery } from '../../domain/history-query';
import { HistoryFilters } from './history-filters';

const query: HistoryQuery = {
  from: '2026-09-24',
  to: '2026-09-30',
  suppliers: [],
  origin: 'Haifa',
  destination: '',
  includeFailures: false,
  sortBy: 'date',
  sortDirection: 'desc',
  page: 1,
  pageSize: 20,
};

describe('HistoryFilters [HC1][H2]', () => {
  let fixture: ComponentFixture<HistoryFilters>;
  let element: HTMLElement;
  const emitted: string[] = [];

  beforeEach(async () => {
    emitted.length = 0;
    fixture = TestBed.createComponent(HistoryFilters);
    fixture.componentRef.setInput('query', query);
    fixture.componentRef.setInput('today', '2026-09-30');
    fixture.componentRef.setInput('originText', 'Hai');
    fixture.componentRef.setInput('destinationText', '');
    fixture.componentRef.setInput('suppliers', [
      { id: 'albatross-freight', name: 'Albatross Freight' },
    ]);
    const component = fixture.componentInstance;
    component.fromChange.subscribe((value) => emitted.push(`from ${value}`));
    component.toChange.subscribe((value) => emitted.push(`to ${value}`));
    component.originInput.subscribe((value) => emitted.push(`origin ${value}`));
    component.destinationInput.subscribe((value) => emitted.push(`destination ${value}`));
    component.includeFailuresChange.subscribe((value) => emitted.push(`failures ${String(value)}`));
    await fixture.whenStable();
    element = fixture.nativeElement as HTMLElement;
  });

  function input(id: string): HTMLInputElement {
    const found = element.querySelector(`#${id}`);
    if (!(found instanceof HTMLInputElement)) {
      throw new Error(`No input has the id "${id}".`);
    }
    return found;
  }

  it('shows the range, keeping its ends in order and the end no later than today', () => {
    expect([input('history-from').value, input('history-from').max]).toEqual([
      '2026-09-24',
      '2026-09-30',
    ]);
    expect([input('history-to').value, input('history-to').min, input('history-to').max]).toEqual([
      '2026-09-30',
      '2026-09-24',
      '2026-09-30',
    ]);
  });

  it('shows the location text being typed, not only what the URL has', () => {
    expect(input('history-origin').value).toBe('Hai');
  });

  it('reports each change', () => {
    const from = input('history-from');
    from.value = '2026-09-20';
    from.dispatchEvent(new Event('change'));
    const to = input('history-to');
    to.value = '2026-09-29';
    to.dispatchEvent(new Event('change'));
    const origin = input('history-origin');
    origin.value = 'Haifa';
    origin.dispatchEvent(new Event('input'));
    const destination = input('history-destination');
    destination.value = 'Rot';
    destination.dispatchEvent(new Event('input'));
    element.querySelector<HTMLInputElement>('input[type="checkbox"]')?.click();

    expect(emitted).toEqual([
      'from 2026-09-20',
      'to 2026-09-29',
      'origin Haifa',
      'destination Rot',
      'failures true',
    ]);
  });

  it('puts the date in use back when a date field is cleared or impossible', () => {
    const from = input('history-from');
    from.value = '';
    from.dispatchEvent(new Event('change'));
    const to = input('history-to');
    to.value = '275759-09-28';
    to.dispatchEvent(new Event('change'));

    expect([from.value, to.value]).toEqual(['2026-09-24', '2026-09-30']);
    expect(emitted).toEqual([]);
  });

  it('labels every control', () => {
    const labelled = ['history-from', 'history-to', 'history-origin', 'history-destination'].map(
      (id) => element.querySelector(`label[for="${id}"]`)?.textContent.trim(),
    );
    expect(labelled).toEqual(['From date', 'To date', 'Origin', 'Destination']);
    expect(element.querySelector('[role="combobox"]')?.getAttribute('aria-labelledby')).toBe(
      'history-suppliers-label',
    );
  });
});
