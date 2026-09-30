/** One row of `GET /api/history`, as the API sends it. */
export interface HistoryItemDto {
  readonly id: string;
  readonly searchId: string;
  /** ISO-8601 UTC. */
  readonly receivedAt: string;
  readonly origin: string;
  readonly destination: string;
  readonly shipDateFrom: string;
  readonly shipDateTo: string;
  readonly supplierId: string;
  readonly supplierName: string;
  readonly outcome: 'Succeeded' | 'Failed' | 'TimedOut' | 'Cancelled';
  readonly price: { readonly amount: number; readonly currency: string } | null;
  readonly responseTimeMs: number;
  readonly errorCode: string | null;
}

/** The body of `GET /api/history`. */
export interface HistoryPageDto {
  readonly items: readonly HistoryItemDto[];
  readonly page: number;
  readonly pageSize: number;
  readonly totalCount: number;
}
