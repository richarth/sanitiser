# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] - Unreleased

This release splits the single `Umbraco.Community.Sanitiser` package into a family of packages and
lays the groundwork for pluggable personal-data replacement (e.g. Faker- or AI-based).

### Added

- **Package family.** The project is now split into:
  - `Umbraco.Community.Sanitiser.Core` — the `ISanitiser` extension point, orchestration, and the
    `DatabaseTableSanitiser` / `DirectorySanitiser` base classes.
  - `Umbraco.Community.Sanitiser.Users` — removes/anonymises backoffice users.
  - `Umbraco.Community.Sanitiser.Members` — removes/anonymises members.
  - `Umbraco.Community.Sanitiser` — meta-package that installs Core, Users and Members, so existing installs
    upgrade seamlessly.
  - `Umbraco.Community.Sanitiser.Faker` — optional replacer generating realistic fake data via Bogus.
  - `Umbraco.Community.Sanitiser.AI` — optional replacer generating values via Umbraco AI (Umbraco 17.4+/18,
    .NET 10 only). It requests fictional people in batches (`AiReplacement:BatchSize`, default 20) to reduce
    the number of model calls, and makes each record's email and username unique regardless of the model
    output. The `Faker` and `AI` packages are opt-in and not included in the meta-package; install only one
    replacer package, as only one can be active.
- **Anonymise mode.** The Members and Users sanitisers gained a `Mode` setting (`Delete` or `Anonymise`).
  `Delete` (the default) replaces personal data then deletes the record; `Anonymise` replaces personal
  data but keeps the record.
- **Cache-cleanup drift detection.** If records were sanitised but the `umbracoCacheInstruction` table
  contains entries that none of the expected refresher patterns match, a warning is logged — surfacing the
  otherwise-silent case where Umbraco's cache-instruction format has changed and personal data could remain.
- **Dry run.** A `Sanitiser:DryRun` option makes the built-in user and member sanitisers log the records they
  would delete or anonymise without making any changes. Because it is read-only it is also permitted to run in
  Production for a safe preview.
- **Pluggable replacement.** A new `IPersonalDataReplacer` abstraction generates replacement values.
  The default `TemplatePersonalDataReplacer` is driven by the new `Sanitiser:Replacement` config section.
  Register a custom replacer with `builder.SetPersonalDataReplacer<T>()`.
- **Umbraco 13–18 support.** A single package version now targets Umbraco 13 (on .NET 8), Umbraco 15/16
  (on .NET 9), and Umbraco 17/18 (on .NET 10) via per-target-framework dependencies. Dependency floors are
  `[13.0.0,14)`, `[15.0.0,17)`, and `[17.0.0,19)`.
- Repository-level `NuGet.config` pinning restore to nuget.org.
- Test project (`Sanitiser.Tests`, xUnit + NSubstitute). Unit tests cover the domain-exclusion gate, the
  service enable/production-override gating, and the template, Faker and AI replacers. Integration tests wire
  the real Users/Members sanitisers and replacer against a real SQLite database (substituting only the Umbraco
  service boundary) to verify PII is actually replaced in Anonymise mode, scrubbed before deletion in Delete
  mode, that the Super Admin and excluded domains are untouched, and that the EF Core cache-instruction
  cleanup removes only the matching rows.
- `build.yml` CI workflow that builds all target frameworks and runs the tests on push and pull request.
- Playwright end-to-end smoke tests (`e2e/`) that boot the real V17 test site and verify the backoffice is
  served and that sanitisation runs during startup — proving the packages are safe to install in a real
  Umbraco site.
- A `SECURITY.md` describing how to report vulnerabilities, and README guidance on how the ASP.NET Core
  environment affects whether sanitisation runs (only `Production` is protected).

### Changed

- Members are now scrubbed (name, email, username replaced) before deletion, matching the behaviour users
  already had. Previously members were deleted without their lingering audit/log-table values being scrubbed.
- Assembly name typo fixed: `Umbracro.Community.Sanitiser` → `Umbraco.Community.Sanitiser.Core`.
- Shared package metadata and version consolidated into `src/Directory.Build.props`.
- The default template replacement email domain changed from `domain.com` (a real, registered domain) to the
  reserved `example.com`. `DomainsToExclude` documentation and samples now use realistic "your own domain"
  placeholders to avoid confusion with the reserved domain used for generated data.
- The custom `SanitiserDbContext` registration now uses a non-obsolete `AddUmbracoDbContext` overload on
  .NET 10, so the package works on Umbraco 18 where the previous overload was removed.
- The Faker replacer now separates the uniqueness index in generated usernames with an underscore, so two
  records can never merge into the same username.
- NuGet auditing is scoped to direct dependencies (`NuGetAuditMode=direct`).
- Removed an empty `en.xml` language placeholder that the package shipped but never used. Every previously-flagged
  advisory was a transitive Umbraco dependency, governed by the consuming site's Umbraco version rather than
  this library (and several, such as `SQLitePCLRaw` and `MailKit`, have no fixed version even in current
  releases). Auditing of our own direct dependencies remains enabled.

### Migration notes

- **Replacement templates moved.** `EmailTemplate`, `NameTemplate`, and `UserNameTemplate` previously lived
  under `Sanitiser:UsersSanitiser`. They now live in the shared `Sanitiser:Replacement` section and apply to
  all sanitisers. Move any customised templates accordingly; the appsettings shape is otherwise unchanged.
- **Options split (source-breaking for extenders).** Code that injected `IOptions<SanitiserOptions>` to read
  the nested `UsersSanitiser` / `MembersSanitiser` options should now inject the per-strategy options type
  (`IOptions<UsersSanitiserOptions>` / `IOptions<MembersSanitiserOptions>`).
- **Custom options pattern changed.** The previous "subclass `SanitiserOptions`" approach is replaced by
  binding your own options section in a composer, as the built-in strategies now do. See the README.
- **Assembly rename** is binary-breaking for any consumer with a hard assembly reference; the meta-package
  preserves the NuGet upgrade path.
- Requires Umbraco 13.0+ (.NET 8), 15.0+ (.NET 9), or 17.0+ (.NET 10). The optional `AI` package requires
  Umbraco 17.4+/18.

## [0.4.6] and earlier

See the [GitHub releases](https://github.com/richarth/sanitiser/releases) for prior history.
