import { defineConfig } from '@playwright/test';
import { resolve } from 'path';

// Which test site to boot. Repo-root-relative path; override via SITE_PROJECT to target another Umbraco
// version. Each test site references the Sanitiser package with sanitisation enabled, so booting it
// exercises the real package in a real Umbraco site.
const siteProject = process.env.SITE_PROJECT ?? 'src/Sanitiser.TestSite.V17/Sanitiser.TestSite.v17.csproj';
const projectPath = resolve(process.cwd(), '..', siteProject);
const port = Number(process.env.SITE_PORT ?? 5199);
const baseURL = `http://localhost:${port}`;

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list']],
  use: {
    baseURL,
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
  },
  webServer: {
    command: `dotnet run --project "${projectPath}" --no-launch-profile -c Release`,
    url: `${baseURL}/umbraco`,
    reuseExistingServer: !process.env.CI,
    // Umbraco's first-run unattended install can take a while.
    timeout: 240_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: baseURL,
    },
  },
});
