import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import type { FinalBadge, SearchProgress } from '../../domain/search-summary';

const BADGE_CLASSES: Record<FinalBadge['kind'], string> = {
  completed: 'bg-emerald-50 text-emerald-800 ring-emerald-200',
  timedOut: 'bg-amber-50 text-amber-900 ring-amber-200',
  cancelled: 'bg-slate-100 text-slate-700 ring-slate-200',
  error: 'bg-rose-50 text-rose-800 ring-rose-200',
};

/**
 * Search progress (CL4): how many suppliers answered, who is still pending, a bar toward the
 * deadline, and the final state, announced politely to screen readers.
 */
@Component({
  selector: 'ph-progress-panel',
  host: { class: 'block' },
  templateUrl: './progress-panel.html',
  styleUrl: './progress-panel.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProgressPanel {
  readonly searching = input.required<boolean>();
  readonly progress = input.required<SearchProgress>();
  readonly badge = input<FinalBadge | null>(null);
  readonly maxDurationMs = input<number | null>(null);

  protected readonly badgeClasses = BADGE_CLASSES;
}
