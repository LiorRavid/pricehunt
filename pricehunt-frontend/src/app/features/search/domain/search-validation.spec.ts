import {
  validateEndDate,
  validateLocation,
  validateRoute,
  validateStartDate,
  validateSupplierSelection,
} from './search-validation';

describe('search validation [CL1]', () => {
  it.each([
    ['', 'A location is required.'],
    ['   ', 'A location is required.'],
    ['a'.repeat(101), 'A location can have at most 100 characters.'],
  ])('rejects the location %j', (value, message) => {
    expect(validateLocation(value)).toBe(message);
  });

  it('accepts a trimmed location of up to 100 characters', () => {
    expect(validateLocation(`  ${'a'.repeat(100)}  `)).toBeNull();
  });

  it('rejects the same origin and destination, ignoring case and spaces', () => {
    expect(validateRoute('Haifa', ' haifa ')).toBe('The destination must differ from the origin.');
    expect(validateRoute('Haifa', 'Rotterdam')).toBeNull();
    expect(validateRoute('', '')).toBeNull();
  });

  it('requires both dates and an end that is not before the start', () => {
    expect(validateStartDate('')).toBe('A start date is required.');
    expect(validateStartDate('2026-10-01')).toBeNull();
    expect(validateEndDate('2026-10-01', '')).toBe('An end date is required.');
    expect(validateEndDate('2026-10-02', '2026-10-01')).toBe(
      "The end date can't be before the start date.",
    );
    expect(validateEndDate('2026-10-01', '2026-10-01')).toBeNull();
    expect(validateEndDate('', '2026-10-01')).toBeNull();
  });

  it('requires at least one supplier', () => {
    expect(validateSupplierSelection([])).toBe('Select at least one supplier.');
    expect(validateSupplierSelection(['albatross-freight'])).toBeNull();
  });
});
