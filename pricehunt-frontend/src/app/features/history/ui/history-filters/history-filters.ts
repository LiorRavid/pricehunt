import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
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
}
