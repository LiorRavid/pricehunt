const formatters = new Map<string, Intl.NumberFormat>();

/** Formats an amount in its currency, for example `$1,234.56`. */
export function formatMoney(amount: number, currency: string): string {
  let formatter = formatters.get(currency);
  if (!formatter) {
    formatter = new Intl.NumberFormat('en-US', { style: 'currency', currency });
    formatters.set(currency, formatter);
  }

  return formatter.format(amount);
}
