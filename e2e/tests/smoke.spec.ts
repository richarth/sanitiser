import { test, expect } from '@playwright/test';
import { readFileSync, readdirSync, existsSync, statSync } from 'fs';
import { join } from 'path';

// These tests boot the real V17 test site (via the webServer in playwright.config.ts) with the Sanitiser
// packages installed and sanitisation enabled. If the package threw during startup the site would fail to
// boot and the backoffice would not be served, so a green run proves the package is safe to install in a
// real Umbraco 17 site. Data-removal correctness is covered by the integration tests in Sanitiser.Tests.

test('the backoffice endpoint is served', async ({ request }) => {
  const response = await request.get('/umbraco');
  expect(response.ok()).toBeTruthy();
});

test('the backoffice renders in the browser', async ({ page }) => {
  await page.goto('/umbraco');
  await expect(page.locator('body')).toBeVisible();
});

test('sanitisation ran during startup', () => {
  const logsDir = join(process.cwd(), '..', 'src', 'Sanitiser.TestSite.V17', 'umbraco', 'Logs');
  expect(existsSync(logsDir), `expected Umbraco logs at ${logsDir}`).toBeTruthy();

  // Select by modification time: log file names carry the machine name, so an alphabetical sort is unreliable
  // when a Logs directory has accumulated files from different machines.
  const latestLog = readdirSync(logsDir)
    .filter((file) => file.endsWith('.json'))
    .map((file) => join(logsDir, file))
    .sort((a, b) => statSync(a).mtimeMs - statSync(b).mtimeMs)
    .at(-1);

  expect(latestLog, 'expected at least one Umbraco trace log').toBeTruthy();
  expect(readFileSync(latestLog!, 'utf8')).toContain('Sanitization finished');
});
