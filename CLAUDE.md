# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

The solution is `src/Sanitiser.sln`; run commands from the repo root.

- Build everything: `dotnet build src/Sanitiser.sln -c Release`
- Run all tests: `dotnet test src/Sanitiser.Tests/Sanitiser.Tests.csproj -c Release`
- Run a subset of tests: add `--filter "FullyQualifiedName~EmailHelperTests"` (or `~Integration` for the integration tests only)
- Pack the NuGet packages: `dotnet pack src/Sanitiser/Sanitiser.csproj -c Release`
- Run a test site: `dotnet run --project src/Sanitiser.TestSite.V17 -c Release` (or `.V13`). Set `ASPNETCORE_ENVIRONMENT=Development`, otherwise the production guard skips sanitisation.
- End-to-end smoke tests: `cd e2e && npm ci && npx playwright install chromium && npm test`

Tests use xUnit + NSubstitute. Integration tests run against a real SQLite database.

## Architecture

Sanitiser removes or anonymises personal data from an Umbraco site on startup (for privacy in non-production
environments). It is a family of NuGet packages under `src/`:

- **Sanitiser.Core** — the extension point and orchestration: `ISanitiser`, `SanitizationService`, the
  `SanitisersCollection(Builder)`, `SanitiserComposer`, `SanitiserOptions`, `SanitiserDbContext`, the
  `IPersonalDataReplacer` abstraction + template implementation, and the `DatabaseTableSanitiser` /
  `DirectorySanitiser` abstract base classes.
- **Sanitiser** (`src/Sanitiser`, package `Umbraco.Community.Sanitiser`) — the main package: contains the
  built-in User and Member sanitisers (+ their options and composers) and depends on Core. This is what most
  sites install.
- **Sanitiser.Faker / Sanitiser.AI** — optional replacer packages (Bogus / Umbraco AI) that depend on Core.
  Opt-in. Only one replacer can be active at a time.

### Multi-targeting (important)

A single package version supports several Umbraco majors via per-target-framework dependencies in
`Sanitiser.Core.csproj`:

- `net8.0` → Umbraco 13 (`[13.0.0,14)`)
- `net9.0` → Umbraco 15/16 (`[15.0.0,17)`)
- `net10.0` → Umbraco 17/18 (`[17.0.0,19)`)

`Sanitiser.AI` is `net10.0`-only (Umbraco 17.4+/18), because Umbraco.AI only ships there. Code that calls
Umbraco APIs must compile against the *floor* of each range, and those APIs differ across majors — e.g.
`SanitiserComposer` uses `#if NET10_0_OR_GREATER` around `AddUmbracoDbContext` because the older overload was
removed in Umbraco 18. Verify API availability across floors before using it.

### Runtime flow

`SanitiserComposer` registers the service, the DbContext, the default `TemplatePersonalDataReplacer` (via
`TryAddSingleton` so replacer packages can override it regardless of composer order), and a handler for
`UmbracoApplicationStartingNotification`. On startup `SanitizationService` runs — unless disabled, or in
Production without `ProductionOverride`/`DryRun` — iterating every discovered `ISanitiser`. Each strategy uses
the registered `IPersonalDataReplacer` to produce replacement values, then deletes (`Delete` mode) or saves
(`Anonymise` mode) each record, and removes matching rows from `umbracoCacheInstruction`. That cache cleanup
matches Umbraco's internal JSON format with a `LIKE` query and warns if it finds nothing to clean (drift
detection).

### Conventions

- Each strategy/replacer package binds its own options section in its own composer (e.g.
  `Sanitiser:UsersSanitiser`, `Sanitiser:AiReplacement`). Core owns only the top-level `SanitiserOptions`
  (`Enable`, `ProductionOverride`, `DryRun`).
- Custom replacers register with `builder.SetPersonalDataReplacer<T>()`.
- `SanitizationService` builds a `SanitisationContext` (carrying `DryRun` and a logger) and passes it to every
  `ISanitiser.Sanitise(context)`. All built-in sanitisers and the base classes honour `context.DryRun`; a
  custom `ISanitiser` should too.
- `NuGetAuditMode=direct` (in `src/Directory.Build.props`): the flagged transitive vulnerabilities belong to
  Umbraco, not this library. Shared package metadata/version also live there; `NuGet.config` pins restore to
  nuget.org.
- Keep `CHANGELOG.md` updated — the release workflow extracts the tagged version's section into the GitHub
  release.

## Test sites and CI

`Sanitiser.TestSite.V13` (net8/Umbraco 13), `Sanitiser.TestSite.V17` (net10/Umbraco 17) and
`Sanitiser.TestSite.V18` (net10/Umbraco 18) are manual harnesses using unattended install + SQLite; their
`umbraco/` runtime artifacts (database, logs) are not committed. V17 exercises Faker + Anonymise mode; V18
exercises the default template replacer + Delete mode. `build.yml` builds all TFMs and runs the
unit/integration tests on push and PR. `e2e.yml` runs the Playwright smoke tests manually or nightly (kept off
the push/PR path deliberately).
