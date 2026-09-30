import type { Money } from './search-criteria';

interface RowBase {
  readonly supplierId: string;
  readonly supplierName: string;
}

/** One supplier's slot in the live results, from the moment the search starts. */
export type ResultRow =
  | (RowBase & { readonly state: 'pending' })
  | (RowBase & { readonly state: 'quoted'; readonly price: Money; readonly responseTimeMs: number })
  | (RowBase & {
      readonly state: 'failed';
      readonly errorMessage: string;
      readonly responseTimeMs: number;
    })
  | (RowBase & { readonly state: 'noResponse' });

const groupOrder: Record<ResultRow['state'], number> = {
  quoted: 0,
  failed: 1,
  pending: 2,
  noResponse: 2,
};

/**
 * Cheapest first; equal prices by response time, then by supplier name. Failed suppliers follow the
 * priced ones, and suppliers still pending (or that never answered) come last.
 */
export function compareResultRows(a: ResultRow, b: ResultRow): number {
  return (
    groupOrder[a.state] - groupOrder[b.state] ||
    compareQuotes(a, b) ||
    a.supplierName.localeCompare(b.supplierName, 'en') ||
    a.supplierId.localeCompare(b.supplierId, 'en')
  );
}

export function sortResultRows(rows: readonly ResultRow[]): ResultRow[] {
  return [...rows].sort(compareResultRows);
}

function compareQuotes(a: ResultRow, b: ResultRow): number {
  return a.state === 'quoted' && b.state === 'quoted'
    ? a.price.amount - b.price.amount || a.responseTimeMs - b.responseTimeMs
    : 0;
}
