import { expect, test } from './support/fixtures';
import { SearchScreen } from './support/search-screen';

test('invalid input blocks the search and says why next to the field [P1][CL1]', async ({
  page,
}) => {
  const screen = new SearchScreen(page);
  let searches = 0;
  page.on('request', (request) => {
    if (request.url().endsWith('/api/searches')) {
      searches++;
    }
  });
  await screen.open();

  await expect(screen.searchButton).toBeDisabled();

  await screen.origin.fill('Haifa');
  await screen.destination.fill(' haifa ');
  await screen.destination.blur();
  // The message is shown and tied to the field, so a screen reader reads it with the field.
  await expect(screen.destination).toHaveAccessibleDescription(
    'The destination must differ from the origin.',
  );
  await expect(screen.destination).toHaveAttribute('aria-invalid', 'true');
  await expect(screen.searchButton).toBeDisabled();
  // Enter in a field doesn't get round the rules either.
  await screen.destination.press('Enter');

  await screen.destination.fill('Rotterdam');
  await expect(screen.searchButton).toBeEnabled();

  const shipBy = page.getByRole('textbox', { name: 'Ship by' });
  const shipByValue = await shipBy.inputValue();
  await shipBy.fill('2020-01-01');
  await shipBy.blur();
  await expect(shipBy).toHaveAccessibleDescription("The end date can't be before the start date.");
  await expect(screen.searchButton).toBeDisabled();
  await shipBy.fill(shipByValue);
  await expect(screen.searchButton).toBeEnabled();

  await page.getByRole('button', { name: 'Select none' }).click();
  await expect(page.getByRole('group', { name: 'Suppliers' })).toHaveAccessibleDescription(
    'Select at least one supplier.',
  );
  await expect(screen.searchButton).toBeDisabled();

  expect(searches).toBe(0);
});
