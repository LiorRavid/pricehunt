import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { formatDuration } from '../../../../shared/formatting/format-duration';
import { formatMoney } from '../../../../shared/formatting/format-money';
import type { ResultRow } from '../../domain/result-row';
import { RankPosition } from '../rank-position';

const ROW_CLASSES: Record<ResultRow['state'], string> = {
  quoted: 'bg-white text-slate-900',
  pending: 'bg-white text-slate-900',
  failed: 'bg-slate-50 text-slate-500',
  noResponse: 'bg-slate-50 text-slate-500',
};

/**
 * The live results (CL2, CL3): one fixed-height slot per selected supplier, in sorted DOM order,
 * tracked by supplier id so rows are moved, never re-created.
 */
@Component({
  selector: 'ph-results-list',
  host: { class: 'block' },
  imports: [RankPosition],
  templateUrl: './results-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResultsList {
  readonly rows = input.required<readonly ResultRow[]>();

  protected readonly listHeight = computed(
    () => `calc(var(--spacing-result-row) * ${String(this.rows().length)})`,
  );

  protected readonly rowClasses = ROW_CLASSES;
  protected readonly formatMoney = formatMoney;
  protected readonly formatDuration = formatDuration;
}
