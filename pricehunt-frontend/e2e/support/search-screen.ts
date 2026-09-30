import { randomUUID } from 'node:crypto';
import { expect, type Locator, type Page } from '@playwright/test';

export const SUPPLIERS = [
  'Albatross Freight',
  'Bramblewood Cargo',
  'Cobalt Harbor Lines',
  'Driftwood Shipping',
  'Emberline Logistics',
  'Foxglove Freightways',
  'Gullwing Transport',
] as const;

/** The supplier that never answers, so a search that includes it ends at the deadline. */
export const SILENT_SUPPLIER = 'Gullwing Transport';

/** A location no other test uses, so each test can find its own searches in the history. */
export function uniqueLocation(name: string): string {
  return `${name} ${randomUUID().slice(0, 8)}`;
}

export interface ResultRow {
  readonly supplier: string;
  /** Null until the supplier quotes, and for failed or silent suppliers. */
  readonly price: number | null;
  readonly text: string;
}

/** The live search screen, reached through roles and accessible names only. */
export class SearchScreen {
  readonly origin: Locator;
  readonly destination: Locator;
  readonly searchButton: Locator;
  readonly cancelButton: Locator;
  readonly results: Locator;
  readonly outcome: Locator;
  readonly progress: Locator;

  constructor(readonly page: Page) {
    this.origin = page.getByRole('textbox', { name: 'From', exact: true });
    this.destination = page.getByRole('textbox', { name: 'To', exact: true });
    this.searchButton = page.getByRole('button', { name: 'Search', exact: true });
    this.cancelButton = page.getByRole('button', { name: 'Cancel', exact: true });
    this.results = page.getByRole('list', { name: 'Search results' });
    this.outcome = page.getByRole('status');
    this.progress = page.getByRole('region', { name: 'Search progress' });
  }

  async open(): Promise<void> {
    await this.page.goto('/search');
    await expect(this.supplier(SUPPLIERS[0])).toBeChecked();
  }

  supplier(name: string): Locator {
    return this.page.getByRole('checkbox', { name, exact: true });
  }

  async chooseSuppliers(names: readonly string[]): Promise<void> {
    await this.page.getByRole('button', { name: 'Select none' }).click();
    for (const name of names) {
      await this.supplier(name).check();
    }
  }

  async search(origin: string, destination: string): Promise<void> {
    await this.origin.fill(origin);
    await this.destination.fill(destination);
    await this.searchButton.click();
  }

  rows(): Locator {
    return this.results.getByRole('listitem');
  }

  /**
   * The rows in the order they're shown, read from one accessibility snapshot of the list. The
   * snapshot is taken in a single step; reading item by item takes two round trips (find the
   * items, then read their text), so a re-sort landing in between would pair the old order with
   * the new text.
   */
  async readRows(): Promise<ResultRow[]> {
    const snapshot = await this.results.ariaSnapshot();
    return snapshot
      .split(/^\s*- listitem\b.*$/m)
      .slice(1)
      .map((text) => {
        const price = /\$([\d,]+\.\d{2})/.exec(text)?.[1];
        return {
          supplier: /paragraph: (.+)/.exec(text)?.[1]?.trim() ?? '',
          price: price === undefined ? null : Number(price.replaceAll(',', '')),
          text,
        };
      });
  }

  /** How many suppliers have answered so far, or null before the search starts. */
  async respondedCount(): Promise<number | null> {
    const text = await this.progress.innerText();
    const count = /(\d+) of \d+ suppliers responded/.exec(text)?.[1];
    return count === undefined ? null : Number(count);
  }

  /** Whether the search has ended, which the status region announces. */
  async hasEnded(): Promise<boolean> {
    return (await this.outcome.innerText()).trim() !== '';
  }
}
