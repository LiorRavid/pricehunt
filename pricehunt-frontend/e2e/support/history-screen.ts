import { expect, type Locator, type Page } from '@playwright/test';

export interface HistoryRow {
  readonly date: string;
  readonly route: string;
  readonly supplier: string;
  readonly price: string;
  readonly responseTime: string;
}

/** The price history screen, reached through roles and accessible names only. */
export class HistoryScreen {
  readonly suppliers: Locator;
  readonly origin: Locator;
  readonly destination: Locator;
  readonly includeFailures: Locator;
  readonly table: Locator;
  readonly pagination: Locator;
  readonly pageSize: Locator;

  constructor(readonly page: Page) {
    this.suppliers = page.getByRole('combobox', { name: 'Suppliers' });
    this.origin = page.getByRole('searchbox', { name: 'Origin' });
    this.destination = page.getByRole('searchbox', { name: 'Destination' });
    this.includeFailures = page.getByRole('checkbox', { name: /Include failures/ });
    this.table = page.getByRole('table', { name: 'Price history' });
    this.pagination = page.getByRole('navigation', { name: 'Pagination' });
    this.pageSize = page.getByRole('combobox', { name: 'Rows per page' });
  }

  /** Opens the history at `path` and waits for the first page. */
  async open(path = '/history'): Promise<void> {
    await this.reloadsAfter(async () => {
      await this.page.goto(path);
    });
  }

  /**
   * Runs `action`, then waits for the history request it causes and for the table to show the
   * answer. Waiting for the table alone could pass before the new request has even started.
   */
  async reloadsAfter(action: () => Promise<unknown>): Promise<void> {
    const response = this.page.waitForResponse((candidate) =>
      candidate.url().includes('/api/history'),
    );
    await action();
    await response;
    await expect(this.table).toHaveAttribute('aria-busy', 'false');
    await expect(this.pagination.getByRole('status')).toHaveText(/Showing|No results/);
  }

  header(label: string): Locator {
    return this.table.getByRole('columnheader', { name: label, exact: true });
  }

  async sortBy(label: string): Promise<void> {
    await this.reloadsAfter(() => this.header(label).getByRole('button').click());
  }

  async showing(): Promise<string> {
    return this.pagination.getByRole('status').innerText();
  }

  /** The body rows, in the order they're shown. */
  async readRows(): Promise<HistoryRow[]> {
    const rows = await this.table.getByRole('row').all();
    const read: HistoryRow[] = [];
    for (const row of rows.slice(1)) {
      const cells = await row.getByRole('cell').allInnerTexts();
      if (cells.length === 5) {
        const [date = '', route = '', supplier = '', price = '', responseTime = ''] = cells;
        read.push({ date, route, supplier, price, responseTime });
      }
    }
    return read;
  }

  /** Picks exactly `names` in the supplier filter, or clears it when `names` is empty. */
  async chooseSuppliers(names: readonly string[]): Promise<void> {
    const clear = this.page.getByRole('button', { name: 'Clear supplier filter' });
    if (await clear.isVisible()) {
      await this.reloadsAfter(() => clear.click());
    }
    if (names.length === 0) {
      return;
    }
    await this.suppliers.click();
    const listbox = this.page.getByRole('listbox', { name: 'Suppliers' });
    for (const name of names) {
      await this.reloadsAfter(() => listbox.getByRole('option', { name, exact: true }).click());
    }
    await this.page.keyboard.press('Escape');
    await expect(listbox).toBeHidden();
  }
}
