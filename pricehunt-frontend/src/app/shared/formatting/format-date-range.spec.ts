import { formatDateRange } from './format-date-range';

// ICU pads the range dash with thin spaces.
const plain = (text: string) => text.replace(/\s/g, ' ');

describe('formatDateRange', () => {
  it('shares the parts two calendar days have in common', () => {
    expect(plain(formatDateRange('2026-09-30', '2026-10-07'))).toBe('Sep 30 – Oct 7, 2026');
    expect(plain(formatDateRange('2026-12-30', '2027-01-02'))).toBe('Dec 30, 2026 – Jan 2, 2027');
  });

  it('shows a single day once', () => {
    expect(plain(formatDateRange('2026-09-30', '2026-09-30'))).toBe('Sep 30, 2026');
  });
});
