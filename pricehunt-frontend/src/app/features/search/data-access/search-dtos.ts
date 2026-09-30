/** Wire types of `POST /api/searches` (ADR-001). */
export interface StartSearchRequestDto {
  readonly origin: string;
  readonly destination: string;
  readonly fromDate: string;
  readonly toDate: string;
  readonly supplierIds: readonly string[];
}

export interface MoneyDto {
  readonly amount: number;
  readonly currency: string;
}

export interface SearchStartedDto {
  readonly searchId: string;
  readonly suppliers: readonly { readonly id: string; readonly name: string }[];
  readonly startedAt: string;
  readonly deadline: string;
  readonly maxDurationMs: number;
}

export interface QuoteReceivedDto {
  readonly searchId: string;
  readonly supplierId: string;
  readonly price: MoneyDto;
  readonly responseTimeMs: number;
  readonly receivedAt: string;
}

export interface SupplierFailedDto {
  readonly searchId: string;
  readonly supplierId: string;
  readonly errorCode: string;
  readonly errorMessage: string;
  readonly responseTimeMs: number;
  readonly receivedAt: string;
}

export interface SearchCompletedDto {
  readonly searchId: string;
  readonly status: 'Completed' | 'TimedOut' | 'Faulted';
  readonly completedAt: string;
  readonly respondedCount: number;
  readonly succeededCount: number;
  readonly failedCount: number;
  readonly noResponseSupplierIds: readonly string[];
}
