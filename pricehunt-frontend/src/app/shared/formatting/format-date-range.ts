import { startOfLocalDay } from '../dates/local-date';

const formatter = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium' });

/** Formats a range of `yyyy-MM-dd` calendar days, for example `Sep 30 – Oct 7, 2026`. */
export function formatDateRange(from: string, to: string): string {
  return formatter.formatRange(startOfLocalDay(from), startOfLocalDay(to));
}
