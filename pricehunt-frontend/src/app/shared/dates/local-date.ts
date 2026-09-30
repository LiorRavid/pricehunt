/** The local calendar date of `date` as `yyyy-MM-dd`. */
export function toLocalIsoDate(date: Date): string {
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  return `${String(date.getFullYear())}-${month}-${day}`;
}

/** The local calendar date `days` after the one given as `yyyy-MM-dd`. */
export function addDays(isoDate: string, days: number): string {
  const date = startOfLocalDay(isoDate);
  date.setDate(date.getDate() + days);
  return toLocalIsoDate(date);
}

/** Local midnight at the start of the calendar day given as `yyyy-MM-dd`. */
export function startOfLocalDay(isoDate: string): Date {
  // A date-time without an offset is local time; a bare date would be read as UTC.
  return new Date(`${isoDate}T00:00:00`);
}

/** Whether `value` is a real calendar day as `yyyy-MM-dd`, with a four-digit year. */
export function isIsoDate(value: string): boolean {
  // 2026-02-31 doesn't survive the round trip.
  return /^\d{4}-\d{2}-\d{2}$/.test(value) && toLocalIsoDate(startOfLocalDay(value)) === value;
}
