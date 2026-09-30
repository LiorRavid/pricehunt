import { expect, test } from './support/fixtures';
import { HistoryScreen } from './support/history-screen';
import { SearchScreen, uniqueLocation } from './support/search-screen';
import { recordSearchRequests, searchRequests } from './support/search-requests';

const FIRST = ['Albatross Freight', 'Bramblewood Cargo', 'Cobalt Harbor Lines'];
const SECOND = ['Driftwood Shipping', 'Emberline Logistics', 'Foxglove Freightways'];

test.describe('cancellation', () => {
  test('a new search cancels the one before, and nothing from it appears [CL5][SV5][DB1][DB2]', async ({
    page,
  }) => {
    const screen = new SearchScreen(page);
    const firstOrigin = uniqueLocation('Haifa');
    const secondOrigin = uniqueLocation('Ashdod');
    await recordSearchRequests(page);

    await screen.open();
    await screen.chooseSuppliers(FIRST);
    await screen.search(firstOrigin, 'Rotterdam');
    await expect(screen.rows()).toHaveCount(FIRST.length);

    // Straight away, a second search with other suppliers.
    await page.getByRole('button', { name: 'Select none' }).click();
    for (const name of SECOND) {
      await screen.supplier(name).check();
    }
    const pendingAtSwitch = (await screen.readRows())
      .filter((row) => row.text.includes('Waiting for a quote'))
      .map((row) => row.supplier);
    await screen.search(secondOrigin, 'Valencia');
    await expect(screen.rows().filter({ hasText: SECOND[0] })).toBeVisible();

    // From here on, no row of the first search may show up.
    while (!(await screen.hasEnded())) {
      const shown = (await screen.readRows()).map((row) => row.supplier);
      expect(shown.filter((supplier) => FIRST.includes(supplier))).toEqual([]);
    }
    await expect(screen.outcome).toContainText('Completed');
    expect([...(await screen.readRows()).map((row) => row.supplier)].sort()).toEqual(SECOND);

    // The first request was aborted; the second ran to its end.
    await expect
      .poll(() => searchRequests(page))
      .toEqual([
        { origin: firstOrigin, aborted: true, ended: false },
        { origin: secondOrigin, aborted: false, ended: true },
      ]);

    // The history keeps the first search, its unanswered suppliers marked as cancelled.
    expect(pendingAtSwitch.length).toBeGreaterThan(0);
    const history = new HistoryScreen(page);
    await history.open(`/history?origin=${encodeURIComponent(firstOrigin)}&failures=true`);
    const rows = await history.readRows();
    expect([...rows.map((row) => row.supplier)].sort()).toEqual(FIRST);
    for (const row of rows.filter((candidate) => pendingAtSwitch.includes(candidate.supplier))) {
      expect(row.price, `${row.supplier} was pending when the search was replaced`).toContain(
        'Cancelled',
      );
    }
  });

  test('the Cancel button stops the search [CL4][CL5][SV5]', async ({ page }) => {
    const screen = new SearchScreen(page);
    await screen.open();
    await screen.search(uniqueLocation('Haifa'), 'Rotterdam');

    await screen.cancelButton.click();

    await expect(screen.outcome).toHaveText(/Cancelled\s*The search was stopped\./);
    await expect(screen.cancelButton).toBeHidden();
    await expect(screen.searchButton).toBeEnabled();
    const rows = await screen.readRows();
    expect(rows.filter((row) => row.text.includes('Waiting for a quote'))).toEqual([]);
  });
});
