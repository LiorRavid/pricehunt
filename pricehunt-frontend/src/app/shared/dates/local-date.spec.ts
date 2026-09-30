import { addDays, startOfLocalDay, toLocalIsoDate } from './local-date';

describe('local dates', () => {
  it('formats the local calendar date', () => {
    expect(toLocalIsoDate(new Date(2026, 8, 30, 23, 59))).toBe('2026-09-30');
    expect(toLocalIsoDate(new Date(2026, 0, 5, 0, 0))).toBe('2026-01-05');
  });

  it('adds days across month and year boundaries', () => {
    expect(addDays('2026-09-30', 7)).toBe('2026-10-07');
    expect(addDays('2026-12-31', 1)).toBe('2027-01-01');
    expect(addDays('2026-03-01', -1)).toBe('2026-02-28');
  });

  it('returns local midnight for a calendar date', () => {
    const midnight = startOfLocalDay('2026-09-30');

    expect([
      midnight.getFullYear(),
      midnight.getMonth(),
      midnight.getDate(),
      midnight.getHours(),
      midnight.getMinutes(),
    ]).toEqual([2026, 8, 30, 0, 0]);
  });
});
