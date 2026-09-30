const formatter = new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' });

/** Formats an instant in the user's time zone, for example `Sep 30, 2026, 5:45 PM`. */
export function formatDateTime(date: Date): string {
  return formatter.format(date);
}
