import { test as base, expect, type Page } from '@playwright/test';

/** Collects console errors, console warnings and uncaught exceptions while a test runs. */
export class ConsoleWatch {
  private readonly problems: string[] = [];
  private readonly allowed: RegExp[] = [];

  constructor(page: Page) {
    page.on('console', (message) => {
      if (message.type() === 'error' || message.type() === 'warning') {
        this.problems.push(`${message.type()}: ${message.text()}`);
      }
    });
    page.on('pageerror', (error) => {
      this.problems.push(`uncaught: ${error.message}`);
    });
  }

  /** Tolerates messages a test provokes on purpose, such as the browser logging a failed request. */
  allow(pattern: RegExp): void {
    this.allowed.push(pattern);
  }

  unexpected(): string[] {
    return this.problems.filter(
      (problem) => !this.allowed.some((pattern) => pattern.test(problem)),
    );
  }
}

/** Every test fails if the page logs an error or warning it didn't expect. */
export const test = base.extend<{ consoleWatch: ConsoleWatch }>({
  consoleWatch: [
    async ({ page }, use) => {
      const watch = new ConsoleWatch(page);
      await use(watch);
      expect(watch.unexpected(), 'console errors, warnings and uncaught exceptions').toEqual([]);
    },
    { auto: true },
  ],
});

export { expect };
