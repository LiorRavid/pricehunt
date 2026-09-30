import type { SupplierRef } from './search-criteria';

/** How far the search has got. */
export interface SearchProgress {
  /** Suppliers that answered, with a price or a failure. */
  readonly responded: number;
  readonly failed: number;
  readonly total: number;
  /** Suppliers still expected to answer, in result order. */
  readonly pending: readonly SupplierRef[];
}

/** How the search ended, as announced to the user. */
export interface FinalBadge {
  readonly kind: 'completed' | 'timedOut' | 'cancelled' | 'error';
  readonly title: string;
  readonly detail: string;
}
