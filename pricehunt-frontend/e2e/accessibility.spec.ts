import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { runSearch } from './support/api';
import { expect, test } from './support/fixtures';
import { HistoryScreen } from './support/history-screen';
import { SearchScreen, SILENT_SUPPLIER, uniqueLocation } from './support/search-screen';

/** Serious and critical WCAG 2.2 AA violations, one line each. */
async function seriousViolations(page: Page): Promise<string[]> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  return results.violations
    .filter((violation) => violation.impact === 'serious' || violation.impact === 'critical')
    .map(
      (violation) =>
        `${violation.id} (${violation.impact ?? ''}): ${violation.help} — ${violation.nodes
          .map((node) => node.target.join(' '))
          .join(', ')}`,
    );
}

/** Presses Tab until `target` has the focus, as a keyboard user would. */
async function tabTo(page: Page, target: ReturnType<Page['getByRole']>): Promise<void> {
  for (let presses = 0; presses < 40; presses++) {
    if (await target.evaluate((element) => element === document.activeElement)) {
      return;
    }
    await page.keyboard.press('Tab');
  }
  throw new Error('Tab never reached the target.');
}

test.describe('accessibility', () => {
  test('the search screen has no serious or critical violations, before or after a search [CL1][CL4]', async ({
    page,
  }) => {
    const screen = new SearchScreen(page);
    await screen.open();
    expect(await seriousViolations(page)).toEqual([]);

    await screen.supplier(SILENT_SUPPLIER).uncheck();
    await screen.search(uniqueLocation('Haifa'), 'Rotterdam');
    await expect(screen.outcome).toContainText('Completed');
    expect(await seriousViolations(page)).toEqual([]);
  });

  test('the history screen has none either, with the supplier list open [HC1][HC2]', async ({
    page,
    request,
  }) => {
    const origin = uniqueLocation('Haifa');
    await runSearch(request, origin, 'Rotterdam');
    const history = new HistoryScreen(page);
    await history.open(`/history?origin=${encodeURIComponent(origin)}&failures=true`);
    expect(await seriousViolations(page)).toEqual([]);

    await history.suppliers.click();
    await expect(page.getByRole('listbox', { name: 'Suppliers' })).toBeVisible();
    expect(await seriousViolations(page)).toEqual([]);
  });

  test('a search can be run with the keyboard alone [CL1]', async ({ page }) => {
    const screen = new SearchScreen(page);
    await screen.open();

    await tabTo(page, screen.origin);
    await page.keyboard.type(uniqueLocation('Haifa'));
    await page.keyboard.press('Tab');
    await expect(screen.destination).toBeFocused();
    await page.keyboard.type('Rotterdam');
    await page.keyboard.press('Enter');

    await expect(screen.rows()).toHaveCount(7);
    await tabTo(page, screen.cancelButton);
    await page.keyboard.press('Enter');
    await expect(screen.outcome).toContainText('Cancelled');
  });

  test('the history can be filtered and sorted with the keyboard alone [HC1][HC2]', async ({
    page,
  }) => {
    const history = new HistoryScreen(page);
    await history.open();

    await tabTo(page, history.suppliers);
    // Like a user, wait to see the list open before moving on to the next option.
    await page.keyboard.press('ArrowDown');
    const listbox = page.getByRole('listbox', { name: 'Suppliers' });
    const albatross = listbox.getByRole('option', { name: 'Albatross Freight' });
    const bramblewood = listbox.getByRole('option', { name: 'Bramblewood Cargo' });
    await expect(history.suppliers).toHaveAttribute(
      'aria-activedescendant',
      (await albatross.getAttribute('id')) ?? 'albatross',
    );
    await page.keyboard.press('ArrowDown');
    await expect(history.suppliers).toHaveAttribute(
      'aria-activedescendant',
      (await bramblewood.getAttribute('id')) ?? 'bramblewood',
    );
    await history.reloadsAfter(() => page.keyboard.press('Space'));
    await expect(history.suppliers).toHaveText('Bramblewood Cargo');
    await page.keyboard.press('Escape');
    await expect(history.suppliers).toBeFocused();
    await expect(page.getByRole('listbox', { name: 'Suppliers' })).toBeHidden();
    expect(page.url()).toContain('suppliers=bramblewood-cargo');

    await tabTo(page, history.header('Price').getByRole('button'));
    await history.reloadsAfter(() => page.keyboard.press('Enter'));
    await expect(history.header('Price')).toHaveAttribute('aria-sort', 'ascending');
  });
});
