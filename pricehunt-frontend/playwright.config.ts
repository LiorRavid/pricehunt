import { defineConfig, devices } from '@playwright/test';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

/**
 * End-to-end tests against the real API and the Angular dev server, started with the same
 * commands `run.ps1` uses. They get ports of their own, so a running copy of the app is neither
 * reused nor disturbed, and the API builds in Release so it doesn't touch the Debug build a running
 * copy has locked.
 */
const API_PORT = 5081;
const APP_PORT = 4201;

/** Each run starts from an empty database. */
const databasePath = join(tmpdir(), 'pricehunt-e2e', `pricehunt-${String(Date.now())}.db`);

export default defineConfig({
  testDir: './e2e',
  // The scenarios measure the real six-second deadline, so they run one at a time.
  workers: 1,
  fullyParallel: false,
  retries: 0,
  forbidOnly: Boolean(process.env['CI']),
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: `http://localhost:${String(APP_PORT)}`,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },
  projects: [
    {
      name: 'chrome',
      // The installed Google Chrome, so no browser download is needed. Without Chrome, run
      // `npx playwright install chromium` and remove `channel`.
      use: { ...devices['Desktop Chrome'], channel: 'chrome' },
    },
  ],
  webServer: [
    {
      command: `dotnet run --project ../pricehunt-backend/src/PriceHunt.Api --configuration Release --launch-profile http -- --urls http://localhost:${String(API_PORT)}`,
      url: `http://localhost:${String(API_PORT)}/health`,
      reuseExistingServer: false,
      timeout: 240_000,
      env: {
        // Reproducible supplier delays, prices and failures.
        Simulation__Seed: '20260930',
        Database__Path: databasePath,
      },
    },
    {
      command: `npm start -- --port ${String(APP_PORT)} --proxy-config proxy.e2e.conf.json`,
      url: `http://localhost:${String(APP_PORT)}`,
      reuseExistingServer: false,
      timeout: 240_000,
      env: { NG_CLI_ANALYTICS: 'false', NG_FORCE_AUTOCOMPLETE: 'false' },
    },
  ],
});
