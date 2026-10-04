import { Component, signal } from '@angular/core';
import { TestBed, type ComponentFixture } from '@angular/core/testing';
import type { Supplier } from '../../../../shared/suppliers/supplier';
import { SupplierMultiselect } from './supplier-multiselect';

const suppliers: Supplier[] = [
  { id: 'albatross-freight', name: 'Albatross Freight' },
  { id: 'bramblewood-cargo', name: 'Bramblewood Cargo' },
  { id: 'cobalt-harbor-lines', name: 'Cobalt Harbor Lines' },
];

@Component({
  imports: [SupplierMultiselect],
  template: `
    <span id="label">Suppliers</span>
    <ph-supplier-multiselect
      labelledBy="label"
      [suppliers]="suppliers()"
      [selected]="selected()"
      (selectedChange)="changes.push($event)"
    />
  `,
})
class Host {
  readonly suppliers = signal<readonly Supplier[]>(suppliers);
  readonly selected = signal<readonly string[]>([]);
  readonly changes: (readonly string[])[] = [];
}

describe('SupplierMultiselect [HC1]', () => {
  let fixture: ComponentFixture<Host>;
  let element: HTMLElement;

  beforeEach(async () => {
    fixture = TestBed.createComponent(Host);
    element = fixture.nativeElement as HTMLElement;
    await fixture.whenStable();
  });

  function combobox(): HTMLElement {
    const found = element.querySelector('[role="combobox"]');
    if (!(found instanceof HTMLElement)) {
      throw new Error('No combobox rendered.');
    }
    return found;
  }

  async function select(ids: string[]): Promise<void> {
    fixture.componentInstance.selected.set(ids);
    await fixture.whenStable();
  }

  const label = () => combobox().textContent.trim();

  it('is a combobox named by the visible label', () => {
    expect(combobox().getAttribute('aria-labelledby')).toBe('label');
    expect(combobox().getAttribute('aria-expanded')).toBe('false');
    expect(combobox().getAttribute('tabindex')).toBe('0');
  });

  it('sums up the selection in its label', async () => {
    expect(label()).toBe('All suppliers');

    await select(['bramblewood-cargo']);
    expect(label()).toBe('Bramblewood Cargo');

    await select(['cobalt-harbor-lines', 'albatross-freight']);
    expect(label()).toBe('Albatross Freight + 1 more');

    await select(suppliers.map((supplier) => supplier.id));
    expect(label()).toBe('All suppliers');
  });

  it('counts a selection it cannot name yet while the suppliers load', async () => {
    fixture.componentInstance.suppliers.set([]);
    await select(['albatross-freight', 'cobalt-harbor-lines']);

    expect(label()).toBe('2 selected');
  });

  it("counts ids the catalogue doesn't know instead of claiming every supplier", async () => {
    await select(['retired-line']);
    expect(label()).toBe('1 selected');

    await select(['albatross-freight', 'retired-line']);
    expect(label()).toBe('Albatross Freight + 1 more');
  });

  it('opens a multi-select listbox and emits each change of selection', async () => {
    combobox().click();
    await fixture.whenStable();

    const listbox = element.ownerDocument.querySelector('[role="listbox"]');
    expect(combobox().getAttribute('aria-expanded')).toBe('true');
    expect(combobox().getAttribute('aria-haspopup')).toBe('listbox');
    expect(combobox().getAttribute('aria-controls')).toBe(listbox?.id);
    expect(listbox?.getAttribute('aria-multiselectable')).toBe('true');
    const options = Array.from(
      element.ownerDocument.querySelectorAll<HTMLElement>('[role="option"]'),
    );
    expect(options.map((option) => option.textContent.trim())).toEqual(
      suppliers.map((supplier) => supplier.name),
    );

    options[1]?.click();
    await fixture.whenStable();
    options[2]?.click();
    await fixture.whenStable();

    expect(fixture.componentInstance.changes).toEqual([
      ['bramblewood-cargo'],
      ['bramblewood-cargo', 'cobalt-harbor-lines'],
    ]);
    expect(options[1]?.getAttribute('aria-selected')).toBe('true');
  });

  it('clears the selection back to every supplier', async () => {
    await select(['albatross-freight']);
    const clear = element.querySelector<HTMLButtonElement>(
      'button[aria-label="Clear supplier filter"]',
    );

    clear?.click();
    await fixture.whenStable();

    expect(fixture.componentInstance.changes).toEqual([[]]);
    expect(label()).toBe('All suppliers');
    expect(element.querySelector('button[aria-label="Clear supplier filter"]')).toBeNull();
  });
});
