import { sortResultRows, type ResultRow } from './result-row';

const usd = (amount: number) => ({ amount, currency: 'USD' });

const quoted = (supplierName: string, amount: number, responseTimeMs: number): ResultRow => ({
  supplierId: supplierName.toLowerCase(),
  supplierName,
  state: 'quoted',
  price: usd(amount),
  responseTimeMs,
});

const names = (rows: readonly ResultRow[]) => rows.map((row) => row.supplierName);

describe('result row ordering [CL2]', () => {
  it('puts the cheapest quote first', () => {
    const rows = [
      quoted('Charlie', 1500, 1000),
      quoted('Alpha', 900, 3000),
      quoted('Bravo', 1200, 2000),
    ];

    expect(names(sortResultRows(rows))).toEqual(['Alpha', 'Bravo', 'Charlie']);
  });

  it('breaks price ties by response time, then by supplier name', () => {
    const rows = [
      quoted('Delta', 1000, 2000),
      quoted('Bravo', 1000, 1000),
      quoted('Alpha', 1000, 2000),
    ];

    expect(names(sortResultRows(rows))).toEqual(['Bravo', 'Alpha', 'Delta']);
  });

  it('keeps failed suppliers below every quote and pending or silent suppliers last', () => {
    const rows: ResultRow[] = [
      { supplierId: 'gull', supplierName: 'Gull', state: 'noResponse' },
      { supplierId: 'echo', supplierName: 'Echo', state: 'pending' },
      {
        supplierId: 'drift',
        supplierName: 'Drift',
        state: 'failed',
        errorMessage: 'Down.',
        responseTimeMs: 900,
      },
      quoted('Zulu', 2400, 4000),
      { supplierId: 'bravo', supplierName: 'Bravo', state: 'pending' },
    ];

    expect(names(sortResultRows(rows))).toEqual(['Zulu', 'Drift', 'Bravo', 'Echo', 'Gull']);
  });

  it('does not change the input array', () => {
    const rows = [quoted('Bravo', 2, 1), quoted('Alpha', 1, 1)];

    sortResultRows(rows);

    expect(names(rows)).toEqual(['Bravo', 'Alpha']);
  });
});
