import { ChangeDetectionStrategy, Component } from '@angular/core';

/** The price history screen. */
@Component({
  selector: 'ph-history-page',
  template: `<h1 class="text-2xl font-semibold tracking-tight">Price history</h1>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HistoryPage {}
