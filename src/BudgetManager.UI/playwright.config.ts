import { defineConfig } from '@playwright/test';

/**
 * E2E config for the Budget Manager UI.
 *
 * The app is expected to already be running at http://localhost:4200 (the dev Docker stack,
 * `docker/run.ps1`), so no `webServer` is started here. Tests run serially with a single worker
 * because they share one IP against the API's auth rate limiter (10 requests / 60s) and the
 * backend uses a single persistent Postgres database.
 */
export default defineConfig({
  testDir: './e2e',
  testMatch: '**/*.spec.ts',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 10_000 },
  reporter: [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: 'http://localhost:4200',
    headless: true,
    viewport: { width: 1280, height: 900 },
    actionTimeout: 15_000,
    navigationTimeout: 20_000,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [{ name: 'chromium', use: { browserName: 'chromium' } }],
});
