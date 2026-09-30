/**
 * The search rules, mirroring the server's `SearchPlanner` so the form rejects exactly what the API
 * would, with the same messages. Each function returns an error message, or `null` when valid.
 */
export const LOCATION_MAX_LENGTH = 100;

export function validateLocation(value: string): string | null {
  const trimmed = value.trim();
  if (trimmed === '') {
    return 'A location is required.';
  }

  return trimmed.length > LOCATION_MAX_LENGTH
    ? `A location can have at most ${String(LOCATION_MAX_LENGTH)} characters.`
    : null;
}

export function validateRoute(origin: string, destination: string): string | null {
  const from = origin.trim().toUpperCase();
  return from !== '' && from === destination.trim().toUpperCase()
    ? 'The destination must differ from the origin.'
    : null;
}

export function validateStartDate(value: string): string | null {
  return value === '' ? 'A start date is required.' : null;
}

export function validateEndDate(fromDate: string, toDate: string): string | null {
  if (toDate === '') {
    return 'An end date is required.';
  }

  // ISO calendar dates compare correctly as text.
  return fromDate !== '' && toDate < fromDate
    ? "The end date can't be before the start date."
    : null;
}

export function validateSupplierSelection(supplierIds: readonly string[]): string | null {
  return supplierIds.length === 0 ? 'Select at least one supplier.' : null;
}
