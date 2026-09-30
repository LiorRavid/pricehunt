/** What the user searches for. Dates are ISO calendar dates (`yyyy-MM-dd`). */
export interface SearchCriteria {
  readonly origin: string;
  readonly destination: string;
  readonly fromDate: string;
  readonly toDate: string;
  readonly supplierIds: readonly string[];
}

/** A supplier as the search screen shows it. */
export interface SupplierRef {
  readonly id: string;
  readonly name: string;
}

/** A price: `amount` in major units (dollars) of an ISO-4217 currency. */
export interface Money {
  readonly amount: number;
  readonly currency: string;
}
