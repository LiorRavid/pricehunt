import type { Money, SupplierRef } from './search-criteria';

/** The server ended the search this way; a client-side cancel never reaches the stream. */
export type SearchOutcome = 'Completed' | 'TimedOut' | 'Faulted';

/** One event of a streaming search, as the search screen understands it. */
export type SearchEvent =
  | {
      readonly type: 'started';
      readonly searchId: string;
      readonly suppliers: readonly SupplierRef[];
      readonly maxDurationMs: number;
    }
  | {
      readonly type: 'quote';
      readonly searchId: string;
      readonly supplierId: string;
      readonly price: Money;
      readonly responseTimeMs: number;
    }
  | {
      readonly type: 'failure';
      readonly searchId: string;
      readonly supplierId: string;
      readonly errorMessage: string;
      readonly responseTimeMs: number;
    }
  | {
      readonly type: 'completed';
      readonly searchId: string;
      readonly outcome: SearchOutcome;
      readonly noResponseSupplierIds: readonly string[];
    };
