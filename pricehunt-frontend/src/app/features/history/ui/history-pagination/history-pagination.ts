import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { PAGE_SIZES } from '../../domain/history-query';
import type { PageSummary } from '../../domain/page-summary';

/** Paging for the history (HC2): first, previous, next and last, a page size, and the range shown. */
@Component({
  selector: 'ph-history-pagination',
  host: { class: 'block' },
  templateUrl: './history-pagination.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPagination {
  readonly summary = input.required<PageSummary>();
  readonly pageSize = input.required<number>();

  readonly pageChange = output<number>();
  readonly pageSizeChange = output<number>();

  protected readonly pageSizes = PAGE_SIZES;

  protected changePageSize(value: string): void {
    this.pageSizeChange.emit(Number(value));
  }
}
