import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { formatDateRange } from '../../../../shared/formatting/format-date-range';
import { formatDateTime } from '../../../../shared/formatting/format-date-time';
import { formatDuration } from '../../../../shared/formatting/format-duration';
import { formatMoney } from '../../../../shared/formatting/format-money';
import type { HistoryItem, ResponseOutcome } from '../../domain/history-item';
import type { HistorySortField, SortDirection } from '../../domain/history-query';

interface Column {
  readonly field: HistorySortField;
  readonly label: string;
  readonly numeric: boolean;
}

const COLUMNS: readonly Column[] = [
  { field: 'date', label: 'Date', numeric: false },
  { field: 'route', label: 'Route', numeric: false },
  { field: 'supplier', label: 'Supplier', numeric: false },
  { field: 'price', label: 'Price', numeric: true },
  { field: 'responseTime', label: 'Response time', numeric: true },
];

const OUTCOME_LABELS: Record<ResponseOutcome, string> = {
  Succeeded: 'Quoted',
  Failed: 'Failed',
  TimedOut: 'Timed out',
  Cancelled: 'Cancelled',
};

const OUTCOME_CLASSES: Record<ResponseOutcome, string> = {
  Succeeded: 'bg-emerald-50 text-emerald-800',
  Failed: 'bg-rose-50 text-rose-800',
  TimedOut: 'bg-amber-50 text-amber-900',
  Cancelled: 'bg-slate-100 text-slate-700',
};

/**
 * The price history table (HC2): every header sorts, a skeleton shows the first load, and the
 * previous rows stay, dimmed, while the next page loads.
 */
@Component({
  selector: 'ph-history-table',
  host: { class: 'block' },
  templateUrl: './history-table.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryTable {
  /** Undefined until the first page arrives. */
  readonly items = input.required<readonly HistoryItem[] | undefined>();
  readonly loading = input.required<boolean>();
  readonly errorMessage = input.required<string | null>();
  readonly sortBy = input.required<HistorySortField>();
  readonly sortDirection = input.required<SortDirection>();

  readonly sortChange = output<HistorySortField>();
  readonly retry = output();

  protected readonly columns = COLUMNS;
  protected readonly outcomeLabels = OUTCOME_LABELS;
  protected readonly outcomeClasses = OUTCOME_CLASSES;
  protected readonly skeletonRows = [1, 2, 3, 4, 5];
  protected readonly formatDateTime = formatDateTime;
  protected readonly formatDateRange = formatDateRange;
  protected readonly formatMoney = formatMoney;
  protected readonly formatDuration = formatDuration;

  protected ariaSort(field: HistorySortField): 'ascending' | 'descending' | 'none' {
    if (field !== this.sortBy()) {
      return 'none';
    }

    return this.sortDirection() === 'asc' ? 'ascending' : 'descending';
  }
}
