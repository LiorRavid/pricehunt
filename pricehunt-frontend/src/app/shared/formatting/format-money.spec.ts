import { formatMoney } from './format-money';

describe('formatMoney', () => {
  it('formats money in its currency with two decimals', () => {
    expect(formatMoney(1234.5, 'USD')).toBe('$1,234.50');
    expect(formatMoney(880, 'EUR')).toBe('€880.00');
  });
});
