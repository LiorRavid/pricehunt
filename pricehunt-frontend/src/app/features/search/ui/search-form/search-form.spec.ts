import { TestBed, type ComponentFixture } from '@angular/core/testing';
import { addDays, toLocalIsoDate } from '../../../../shared/dates/local-date';
import { SearchForm, type SearchSubmission } from './search-form';

const suppliers = [
  { id: 'albatross', name: 'Albatross Freight' },
  { id: 'bramble', name: 'Bramblewood Cargo' },
  { id: 'gullwing', name: 'Gullwing Transport' },
];

describe('SearchForm [CL1]', () => {
  let fixture: ComponentFixture<SearchForm>;
  let element: HTMLElement;

  beforeEach(async () => {
    fixture = TestBed.createComponent(SearchForm);
    fixture.componentRef.setInput('suppliers', suppliers);
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  function query<T extends Element>(selector: string, type: abstract new () => T): T {
    const found = element.querySelector(selector);
    if (!(found instanceof type)) {
      throw new Error(`No ${type.name} matches "${selector}".`);
    }
    return found;
  }

  const input = (id: string) => query(`#${id}`, HTMLInputElement);
  const checkboxes = () =>
    Array.from(element.querySelectorAll<HTMLInputElement>('input[type="checkbox"]'));
  const searchButton = () => query('button[type="submit"]', HTMLButtonElement);
  const findButton = (label: string) =>
    Array.from(element.querySelectorAll('button')).find(
      (candidate) => candidate.textContent.trim() === label,
    );

  function button(label: string): HTMLButtonElement {
    const found = findButton(label);
    if (found === undefined) {
      throw new Error(`No button is labelled "${label}".`);
    }
    return found;
  }

  async function type(id: string, value: string): Promise<void> {
    const field = input(id);
    field.value = value;
    field.dispatchEvent(new Event('input'));
    field.dispatchEvent(new Event('blur'));
    await fixture.whenStable();
  }

  it('selects every supplier and suggests a week from today by default', () => {
    const today = toLocalIsoDate(new Date());

    expect(checkboxes().map((checkbox) => checkbox.checked)).toEqual([true, true, true]);
    expect(input('from-date').value).toBe(today);
    expect(input('to-date').value).toBe(addDays(today, 7));
  });

  it('keeps Search disabled until the form is valid', async () => {
    expect(searchButton().disabled).toBe(true);

    await type('origin', 'Haifa');
    await type('destination', 'Rotterdam');

    expect(searchButton().disabled).toBe(false);
  });

  it('shows the same errors as the server once a field is touched', async () => {
    expect(element.querySelector('#origin-error')).toBeNull();

    await type('origin', 'Haifa');
    await type('destination', ' haifa ');

    expect(element.querySelector('#destination-error')?.textContent).toContain(
      'The destination must differ from the origin.',
    );
    expect(input('destination').getAttribute('aria-invalid')).toBe('true');
    expect(input('destination').getAttribute('aria-describedby')).toBe('destination-error');
  });

  it('rejects an end date before the start date', async () => {
    await type('from-date', '2026-10-08');
    await type('to-date', '2026-10-01');

    expect(element.querySelector('#to-date-error')?.textContent).toContain(
      "The end date can't be before the start date.",
    );
  });

  it('requires at least one supplier', async () => {
    await type('origin', 'Haifa');
    await type('destination', 'Rotterdam');

    button('Select none').click();
    await fixture.whenStable();

    expect(checkboxes().every((checkbox) => !checkbox.checked)).toBe(true);
    expect(element.querySelector('#suppliers-error')?.textContent).toContain(
      'Select at least one supplier.',
    );
    expect(searchButton().disabled).toBe(true);

    button('Select all').click();
    await fixture.whenStable();

    expect(checkboxes().every((checkbox) => checkbox.checked)).toBe(true);
    expect(searchButton().disabled).toBe(false);
  });

  it('submits trimmed criteria with the chosen suppliers', async () => {
    const submissions: SearchSubmission[] = [];
    fixture.componentInstance.searchRequested.subscribe((submission) =>
      submissions.push(submission),
    );
    await type('origin', '  Haifa ');
    await type('destination', 'Rotterdam');
    checkboxes()[1]?.click();
    await fixture.whenStable();

    searchButton().click();
    await fixture.whenStable();

    expect(submissions).toHaveLength(1);
    expect(submissions[0]?.criteria).toMatchObject({
      origin: 'Haifa',
      destination: 'Rotterdam',
      supplierIds: ['albatross', 'gullwing'],
    });
    expect(submissions[0]?.suppliers.map((supplier) => supplier.id)).toEqual([
      'albatross',
      'gullwing',
    ]);
  });

  it('offers Cancel only while searching', async () => {
    const cancelled = vi.fn();
    fixture.componentInstance.cancelRequested.subscribe(cancelled);
    expect(findButton('Cancel')).toBeUndefined();

    fixture.componentRef.setInput('searching', true);
    await fixture.whenStable();
    button('Cancel').click();

    expect(cancelled).toHaveBeenCalledOnce();
  });
});
