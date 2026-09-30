import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { SupplierCatalog } from '../../shared/suppliers/supplier-catalog';
import { SearchStore } from './state/search-store';
import { ProgressPanel } from './ui/progress-panel/progress-panel';
import { ResultsList } from './ui/results-list/results-list';
import { SearchForm, type SearchSubmission } from './ui/search-form/search-form';

/** The live search screen: the form, the progress and the streaming results. */
@Component({
  selector: 'ph-search-page',
  host: { class: 'block' },
  imports: [SearchForm, ProgressPanel, ResultsList],
  templateUrl: './search-page.html',
  providers: [SearchStore],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchPage {
  protected readonly store = inject(SearchStore);
  protected readonly catalog = inject(SupplierCatalog).suppliers;

  // A resource throws when its value is read in the error state.
  protected readonly suppliers = computed(() =>
    this.catalog.hasValue() ? this.catalog.value() : [],
  );

  protected startSearch(submission: SearchSubmission): void {
    this.store.search(submission.criteria, submission.suppliers);
  }

  protected cancelSearch(): void {
    this.store.cancel();
  }

  protected reloadSuppliers(): void {
    this.catalog.reload();
  }
}
