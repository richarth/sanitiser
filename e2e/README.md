# End-to-end smoke tests

Playwright smoke tests that boot the **V17 test site** (which references the Sanitiser strategy packages and
Faker, with sanitisation enabled) and verify that:

- the Umbraco backoffice endpoint is served,
- the backoffice renders in a browser, and
- sanitisation actually ran during startup (asserted from the Umbraco trace log).

If the package threw during startup, the site would not boot and these tests would fail — so this is a smoke
test that the packages are safe to install in a real Umbraco 17 site. The correctness of *what* is
deleted/anonymised is covered by the integration tests in `src/Sanitiser.Tests`.

## Running

```bash
cd e2e
npm install
npm run install-browsers   # one-time Playwright browser download
npm test
```

Playwright starts the test site itself (see `webServer` in `playwright.config.ts`) on
`http://localhost:5199`, waits for the backoffice to respond, runs the tests, then shuts it down. The first
run performs Umbraco's unattended install, which can take a couple of minutes.

## Notes

- The test site runs in the `Development` environment so sanitisation is not skipped by the production guard.
- A full "create a member, restart, confirm it is gone" flow is intentionally **not** implemented here: it
  would require orchestrating a server restart between seeding and verification, which is brittle. The
  integration tests already prove that behaviour against a real database.
