import { formatDateTime } from './format-date-time';

// ICU separates some parts with narrow no-break spaces.
const plain = (text: string) => text.replace(/\s/g, ' ');

describe('formatDateTime', () => {
  it('formats an instant in local time with a short time', () => {
    expect(plain(formatDateTime(new Date(2026, 8, 30, 17, 45)))).toBe('Sep 30, 2026, 5:45 PM');
  });
});
