import { ChangeDetectionStrategy, Component } from '@angular/core';

/** The live search screen. */
@Component({
  selector: 'ph-search-page',
  template: `<h1 class="text-2xl font-semibold tracking-tight">Search shipping prices</h1>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPage {}
