import { expect, test } from './support/fixtures';
import { SearchScreen, SILENT_SUPPLIER, SUPPLIERS, uniqueLocation } from './support/search-screen';

test.describe('live search', () => {
  test('results arrive one at a time, cheapest first at every observation [SV1][CL1][CL2][CL3]', async ({
    page,
  }) => {
    const screen = new SearchScreen(page);
    await screen.open();
    await screen.supplier(SILENT_SUPPLIER).uncheck();
    await screen.search(uniqueLocation('Haifa'), 'Rotterdam');

    // A slot for every supplier from the start, so the list never grows or shrinks.
    await expect(screen.rows()).toHaveCount(SUPPLIERS.length - 1);

    const quotedCounts = new Set<number>();
    const listBoxes = new Set<string>();
    const recordListBox = async () => {
      const box = await screen.results.boundingBox();
      listBoxes.add(`${String(Math.round(box?.y ?? -1))}+${String(Math.round(box?.height ?? -1))}`);
    };
    let observations = 0;
    while (!(await screen.hasEnded())) {
      const rows = await screen.readRows();
      const prices = rows.flatMap((row) => (row.price === null ? [] : [row.price]));
      expect(prices, 'quoted prices, top to bottom').toEqual([...prices].sort((a, b) => a - b));
      expect(rows).toHaveLength(SUPPLIERS.length - 1);
      quotedCounts.add(prices.length);
      await recordListBox();
      observations++;
    }

    await expect(screen.outcome).toContainText('Completed');
    const final = (await screen.readRows()).flatMap((row) =>
      row.price === null ? [] : [row.price],
    );
    expect(final).toEqual([...final].sort((a, b) => a - b));
    expect(observations).toBeGreaterThan(10);
    // The quotes came in several steps, not all at once.
    expect(quotedCounts.size).toBeGreaterThanOrEqual(3);
    // Nothing above the list changed height, so it never moved, not even when the search ended.
    await recordListBox();
    expect([...listBoxes], 'results list position and height').toHaveLength(1);
  });

  test('the counter only goes up, and the default selection times out at about 6 seconds [SV3][CL4]', async ({
    page,
  }) => {
    const screen = new SearchScreen(page);
    await screen.open();
    await screen.search(uniqueLocation('Haifa'), 'Rotterdam');
    const started = Date.now();

    let responded = 0;
    while (!(await screen.hasEnded())) {
      const now = (await screen.respondedCount()) ?? 0;
      expect(now, 'suppliers that have responded').toBeGreaterThanOrEqual(responded);
      responded = now;
    }
    const elapsed = Date.now() - started;

    await expect(screen.outcome).toHaveText(/Timed out\s*No response from Gullwing Transport\./);
    expect(elapsed).toBeGreaterThan(5_500);
    expect(elapsed).toBeLessThan(8_000);
    expect(await screen.respondedCount()).toBe(SUPPLIERS.length - 1);
  });

  test('without the silent supplier the search completes [CL4]', async ({ page }) => {
    const screen = new SearchScreen(page);
    await screen.open();
    await screen.supplier(SILENT_SUPPLIER).uncheck();
    await screen.search(uniqueLocation('Haifa'), 'Rotterdam');

    await expect(screen.outcome).toHaveText(/Completed\s*6 of 6 suppliers responded/);
    await expect(screen.cancelButton).toBeHidden();
  });
});
