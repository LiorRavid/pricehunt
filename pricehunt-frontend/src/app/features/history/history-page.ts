import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { SupplierCatalog } from '../../shared/suppliers/supplier-catalog';
import { HistoryStore } from './state/history-store';
import { HistoryFilters } from './ui/history-filters/history-filters';
import { HistoryPagination } from './ui/history-pagination/history-pagination';
import { HistoryTable } from './ui/history-table/history-table';

/** The price history screen: filters, a sortable table and paging, all held in the URL. */
@Component({
  selector: 'ph-history-page',
  host: { class: 'block' },
  imports: [HistoryFilters, HistoryTable, HistoryPagination],
  templateUrl: './history-page.html',
  providers: [HistoryStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPage {
  protected readonly store = inject(HistoryStore);
  private readonly catalog = inject(SupplierCatalog).suppliers;

  // A resource throws when its value is read in the error state.
  protected readonly suppliers = computed(() =>
    this.catalog.hasValue() ? this.catalog.value() : [],
  );

  /** Loads the history again, and the suppliers too if they failed along with it. */
  protected retry(): void {
    this.store.retry();
    if (this.catalog.error()) {
      this.catalog.reload();
    }
  }
}
