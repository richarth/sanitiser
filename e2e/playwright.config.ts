import { defineConfig } from '@playwright/test';

// The V17 test site references the Sanitiser strategy + Faker packages and has sanitisation enabled, so
// booting it exercises the real package in a real Umbraco 17 site.
const port = 5199;
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
    command:
      'dotnet run --project ../src/Sanitiser.TestSite.V17/Sanitiser.TestSite.v17.csproj --no-launch-profile -c Release',
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
