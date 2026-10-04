import {
  ChangeDetectionStrategy,
  Component,
  input,
  output,
  type OutputEmitterRef,
} from '@angular/core';
import { isIsoDate } from '../../../../shared/dates/local-date';
import type { Supplier } from '../../../../shared/suppliers/supplier';
import type { HistoryQuery } from '../../domain/history-query';
import { SupplierMultiselect } from '../supplier-multiselect/supplier-multiselect';

/** The history filters (HC1, H2): a date range, suppliers, locations and failed responses. */
@Component({
  selector: 'ph-history-filters',
  host: { class: 'block' },
  imports: [SupplierMultiselect],
  templateUrl: './history-filters.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryFilters {
  readonly query = input.required<HistoryQuery>();
  /** The latest day the range can end on. */
  readonly today = input.required<string>();
  /** What the location boxes show, which runs ahead of the query while the user types. */
  readonly originText = input.required<string>();
  readonly destinationText = input.required<string>();
  readonly suppliers = input.required<readonly Supplier[]>();

  readonly fromChange = output<string>();
  readonly toChange = output<string>();
  readonly suppliersChange = output<readonly string[]>();
  readonly originInput = output<string>();
  readonly destinationInput = output<string>();
  readonly includeFailuresChange = output<boolean>();

  protected changeFrom(field: HTMLInputElement): void {
    this.reportDate(field, this.query().from, this.fromChange);
  }

  protected changeTo(field: HTMLInputElement): void {
    this.reportDate(field, this.query().to, this.toChange);
  }

  private reportDate(
    field: HTMLInputElement,
    inUse: string,
    change: OutputEmitterRef<string>,
  ): void {
    if (isIsoDate(field.value)) {
      change.emit(field.value);
    } else {
      // The filter ignores a cleared or impossible date, so show the date it still uses.
      field.value = inUse;
    }
  }
}
