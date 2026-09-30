import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  linkedSignal,
  output,
} from '@angular/core';
import { type FieldState, form, FormField, submit, validate } from '@angular/forms/signals';
import { addDays, toLocalIsoDate } from '../../../../shared/dates/local-date';
import type { SearchCriteria, SupplierRef } from '../../domain/search-criteria';
import {
  validateEndDate,
  validateLocation,
  validateRoute,
  validateStartDate,
  validateSupplierSelection,
} from '../../domain/search-validation';

/** What the form submits: the criteria and the suppliers they select. */
export interface SearchSubmission {
  readonly criteria: SearchCriteria;
  readonly suppliers: readonly SupplierRef[];
}

interface SearchFormModel {
  readonly origin: string;
  readonly destination: string;
  readonly fromDate: string;
  readonly toDate: string;
  readonly supplierIds: readonly string[];
}

function toError(kind: string, message: string | null) {
  return message === null ? null : { kind, message };
}

/** The first error of a field the user has touched. */
function shownError(field: FieldState<unknown>): string | null {
  return field.touched() ? (field.errors()[0]?.message ?? null) : null;
}

/** The search form (CL1): locations, shipping dates and suppliers, all selected by default. */
@Component({
  selector: 'ph-search-form',
  host: { class: 'block' },
  imports: [FormField],
  templateUrl: './search-form.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchForm {
  readonly suppliers = input.required<readonly SupplierRef[]>();
  readonly searching = input(false);
  readonly searchRequested = output<SearchSubmission>();
  readonly cancelRequested = output();

  /** Every supplier is selected once the catalogue loads, until the user changes the selection. */
  protected readonly model = linkedSignal<readonly SupplierRef[], SearchFormModel>({
    source: this.suppliers,
    computation: (suppliers, previous) => {
      const today = toLocalIsoDate(new Date());
      const current = previous?.value ?? {
        origin: '',
        destination: '',
        fromDate: today,
        toDate: addDays(today, 7),
        supplierIds: [],
      };
      const keepSelection = previous !== undefined && previous.source.length > 0;
      return {
        ...current,
        supplierIds: keepSelection ? current.supplierIds : suppliers.map((supplier) => supplier.id),
      };
    },
  });

  protected readonly searchForm = form(this.model, (path) => {
    validate(path.origin, ({ value }) => toError('origin', validateLocation(value())));
    validate(path.destination, (field) =>
      toError(
        'destination',
        validateLocation(field.value()) ?? validateRoute(field.valueOf(path.origin), field.value()),
      ),
    );
    validate(path.fromDate, ({ value }) => toError('fromDate', validateStartDate(value())));
    validate(path.toDate, (field) =>
      toError('toDate', validateEndDate(field.valueOf(path.fromDate), field.value())),
    );
    validate(path.supplierIds, ({ value }) =>
      toError('supplierIds', validateSupplierSelection(value())),
    );
  });

  protected readonly selectedIds = computed(() => new Set(this.model().supplierIds));
  protected readonly originError = computed(() => shownError(this.searchForm.origin()));
  protected readonly destinationError = computed(() => shownError(this.searchForm.destination()));
  protected readonly fromDateError = computed(() => shownError(this.searchForm.fromDate()));
  protected readonly toDateError = computed(() => shownError(this.searchForm.toDate()));
  protected readonly suppliersError = computed(() => shownError(this.searchForm.supplierIds()));

  protected toggleSupplier(supplierId: string, selected: boolean): void {
    const chosen = new Set(this.model().supplierIds);
    if (selected) {
      chosen.add(supplierId);
    } else {
      chosen.delete(supplierId);
    }

    this.selectSuppliers(
      this.suppliers()
        .filter((supplier) => chosen.has(supplier.id))
        .map((supplier) => supplier.id),
    );
  }

  protected selectAll(): void {
    this.selectSuppliers(this.suppliers().map((supplier) => supplier.id));
  }

  protected selectNone(): void {
    this.selectSuppliers([]);
  }

  protected async startSearch(event: Event): Promise<void> {
    event.preventDefault();
    await submit(this.searchForm, () => {
      const value = this.model();
      const selected = new Set(value.supplierIds);
      this.searchRequested.emit({
        criteria: { ...value, origin: value.origin.trim(), destination: value.destination.trim() },
        suppliers: this.suppliers().filter((supplier) => selected.has(supplier.id)),
      });
      return Promise.resolve();
    });
  }

  private selectSuppliers(supplierIds: readonly string[]): void {
    const field = this.searchForm.supplierIds();
    field.value.set(supplierIds);
    field.markAsTouched();
  }
}
