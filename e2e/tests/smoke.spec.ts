import { test, expect } from '@playwright/test';
import { readFileSync, readdirSync, existsSync, statSync } from 'fs';
import { dirname, join, resolve } from 'path';

// These tests boot the test site chosen by SITE_PROJECT (via the webServer in playwright.config.ts) with the
// Sanitiser packages installed and sanitisation enabled. If a package threw during startup the site would
// fail to boot and the backoffice would not be served, so a green run proves the packages are safe to install
// in a real Umbraco site of that version. Data-removal correctness is covered by the tests in Sanitiser.Tests.

const siteProject = process.env.SITE_PROJECT ?? 'src/Sanitiser.TestSite.V17/Sanitiser.TestSite.v17.csproj';
const logsDir = resolve(process.cwd(), '..', dirname(siteProject), 'umbraco', 'Logs');

// The Umbraco trace log for the current boot. Log file names carry the machine name, so an alphabetical sort
// is unreliable when a Logs directory has accumulated files from different machines; select by modification
// time instead.
function latestLogContent(): string {
  expect(existsSync(logsDir), `expected Umbraco logs at ${logsDir}`).toBeTruthy();

  const latestLog = readdirSync(logsDir)
    .filter((file) => file.endsWith('.json'))
    .map((file) => join(logsDir, file))
    .sort((a, b) => statSync(a).mtimeMs - statSync(b).mtimeMs)
    .at(-1);

  expect(latestLog, 'expected at least one Umbraco trace log').toBeTruthy();
  return readFileSync(latestLog!, 'utf8');
}

test('the backoffice endpoint is served', async ({ request }) => {
  const response = await request.get('/umbraco');
  expect(response.ok()).toBeTruthy();
});

test('the backoffice renders in the browser', async ({ page }) => {
  await page.goto('/umbraco');
  await expect(page.locator('body')).toBeVisible();
});

test('sanitisation ran during startup', () => {
  expect(latestLogContent()).toContain('Sanitization finished');
});

test('the Forms sanitiser ran against the real Umbraco Forms schema', () => {
  // The Forms sanitiser runs raw DELETEs against the UFRecord* tables. Reaching this log line means every
  // table delete completed against the actual installed Forms version without aborting the run.
  expect(latestLogContent()).toContain('Deleted all Umbraco Forms submissions');
});
