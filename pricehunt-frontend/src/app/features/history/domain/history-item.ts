/** How a selected supplier's part in a search ended. */
export type ResponseOutcome = 'Succeeded' | 'Failed' | 'TimedOut' | 'Cancelled';

export interface Money {
  readonly amount: number;
  readonly currency: string;
}

/** One supplier response in the price history. */
export interface HistoryItem {
  readonly id: string;
  readonly searchId: string;
  /** When the response was recorded. */
  readonly receivedAt: Date;
  readonly origin: string;
  readonly destination: string;
  /** The searched shipping dates, as `yyyy-MM-dd` calendar days. */
  readonly shipDateFrom: string;
  readonly shipDateTo: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly outcome: ResponseOutcome;
  /** Null unless the supplier returned a price. */
  readonly price: Money | null;
  readonly responseTimeMs: number;
}

/** One page of the history, with the number of matches overall. */
export interface HistoryPage {
  readonly items: readonly HistoryItem[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}
