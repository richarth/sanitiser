# Contributing

Thanks for helping improve Sanitiser!

## Branching — please target `develop`

- **`develop`** is the integration branch. **Open your pull requests against `develop`, not `main`.**
- **`main`** is the released/stable branch. Releases are tagged from it, so it only changes via a release PR
  from `develop`.

So: branch off `develop`, make your change, and open a PR back into `develop`.

## Building and testing

From the repository root:

- Build: `dotnet build src/Sanitiser.sln -c Release`
- Test: `dotnet test src/Sanitiser.sln -c Release`
- End-to-end smoke tests (optional, boots a real Umbraco site — needs Node + Docker-free Playwright):
  `cd e2e && npm ci && npx playwright install chromium && npm test`

The build CI runs on every pull request; please make sure it's green. If your change is user-facing, add an
entry to `CHANGELOG.md`.

There's a high-level tour of the codebase (packages, multi-targeting, runtime flow, release process) in
[`CLAUDE.md`](CLAUDE.md) if you want the bigger picture before diving in.

## Reporting issues

Use the [issue tracker](https://github.com/richarth/sanitiser/issues).
