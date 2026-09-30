import { expect, type APIRequestContext } from '@playwright/test';

/** Every supplier except the one that never answers, so a search ends within the deadline. */
const ANSWERING_SUPPLIER_IDS = [
  'albatross-freight',
  'bramblewood-cargo',
  'cobalt-harbor-lines',
  'driftwood-shipping',
  'emberline-logistics',
  'foxglove-freightways',
];

/** Runs a search through the API and waits for its final event, to put data in the history. */
export async function runSearch(
  request: APIRequestContext,
  origin: string,
  destination: string,
): Promise<void> {
  const day = (offset: number) =>
    new Date(Date.now() + offset * 86_400_000).toISOString().slice(0, 10);
  const response = await request.post('/api/searches', {
    headers: { Accept: 'text/event-stream' },
    data: {
      origin,
      destination,
      fromDate: day(0),
      toDate: day(7),
      supplierIds: ANSWERING_SUPPLIER_IDS,
    },
    timeout: 20_000,
  });
  expect(response.ok()).toBe(true);
  expect(await response.text()).toContain('event: search-completed');
}
