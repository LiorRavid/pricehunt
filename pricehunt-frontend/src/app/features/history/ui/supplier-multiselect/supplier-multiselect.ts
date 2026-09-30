import { Combobox, ComboboxPopup, ComboboxWidget } from '@angular/aria/combobox';
import { Listbox, Option } from '@angular/aria/listbox';
import { OverlayModule } from '@angular/cdk/overlay';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
  signal,
} from '@angular/core';
import type { Supplier } from '../../../../shared/suppliers/supplier';

/**
 * Chooses the suppliers the history shows (HC1): an Angular Aria combobox that opens a
 * multi-select listbox. Arrow keys move, Space toggles, Escape closes. Choosing none shows every
 * supplier.
 */
@Component({
  selector: 'ph-supplier-multiselect',
  host: { class: 'block' },
  imports: [Combobox, ComboboxPopup, ComboboxWidget, Listbox, Option, OverlayModule],
  templateUrl: './supplier-multiselect.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierMultiselect {
  readonly suppliers = input.required<readonly Supplier[]>();
  /** The chosen supplier ids; none means every supplier. */
  readonly selected = input.required<readonly string[]>();
  /** The id of the visible label that names the control. */
  readonly labelledBy = input.required<string>();
  readonly selectedChange = output<readonly string[]>();

  protected readonly expanded = signal(false);
  protected readonly selection = linkedSignal(() => [...this.selected()]);
  protected readonly summary = computed(() =>
    describeSelection(this.suppliers(), this.selection()),
  );

  protected choose(ids: string[]): void {
    this.selection.set(ids);
    this.selectedChange.emit(ids);
  }

  protected clear(): void {
    this.choose([]);
  }
}

/** "All suppliers", one name, or the first name and how many more. */
function describeSelection(suppliers: readonly Supplier[], ids: readonly string[]): string {
  if (suppliers.length === 0) {
    return ids.length === 0 ? 'All suppliers' : `${String(ids.length)} selected`;
  }

  const [first, ...others] = suppliers
    .filter((supplier) => ids.includes(supplier.id))
    .map((supplier) => supplier.name);
  if (first === undefined || others.length + 1 === suppliers.length) {
    return 'All suppliers';
  }

  return others.length === 0 ? first : `${first} + ${String(others.length)} more`;
}
