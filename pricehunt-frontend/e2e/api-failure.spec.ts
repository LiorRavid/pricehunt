import { expect, test } from './support/fixtures';
import { HistoryScreen } from './support/history-screen';
import { SearchScreen } from './support/search-screen';

// The browser itself logs every failed request; that's expected here, anything else isn't.
const FAILED_REQUEST = /Failed to load resource: the server responded with a status of 5\d\d/;

test.describe('when the API fails', () => {
  test('the history explains it and loads again on retry [HC1]', async ({ page, consoleWatch }) => {
    consoleWatch.allow(FAILED_REQUEST);
    await page.route('**/api/history**', (route) =>
      route.fulfill({ status: 503, contentType: 'text/plain', body: 'Service Unavailable' }),
    );
    await page.goto('/history');

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('The history could not be loaded. Please try again.');
    await expect(page.getByRole('table')).toHaveCount(0);

    await page.unroute('**/api/history**');
    await new HistoryScreen(page).reloadsAfter(() =>
      alert.getByRole('button', { name: 'Try again' }).click(),
    );
    await expect(alert).toBeHidden();
  });

  test('a search the server rejects ends with a clear error [CL4]', async ({
    page,
    consoleWatch,
  }) => {
    consoleWatch.allow(FAILED_REQUEST);
    await page.route('**/api/searches', (route) =>
      route.fulfill({
        status: 500,
        contentType: 'application/problem+json',
        body: JSON.stringify({
          title: 'An error occurred while processing your request.',
          status: 500,
        }),
      }),
    );
    const screen = new SearchScreen(page);
    await screen.open();
    await screen.search('Haifa', 'Rotterdam');

    await expect(screen.outcome).toHaveText(
      /Error\s*The server could not run the search\. Please try again\./,
    );
    await expect(screen.searchButton).toBeEnabled();
  });

  test('a supplier list that fails to load can be retried [P1]', async ({ page, consoleWatch }) => {
    consoleWatch.allow(FAILED_REQUEST);
    await page.route('**/api/suppliers', (route) =>
      route.fulfill({ status: 503, contentType: 'text/plain', body: 'Service Unavailable' }),
    );
    await page.goto('/search');

    const alert = page.getByRole('alert');
    await expect(alert).toContainText('The suppliers could not be loaded.');
    await page.unroute('**/api/suppliers');
    await alert.getByRole('button', { name: 'Try again' }).click();

    await expect(page.getByRole('checkbox', { name: 'Albatross Freight' })).toBeChecked();
    await expect(alert).toBeHidden();
  });
});
