# Security Policy

## Supported versions

Security fixes are made against the latest published version of the Sanitiser packages. Please make sure you
are on the latest version before reporting an issue.

## Reporting a vulnerability

Please report security vulnerabilities privately rather than opening a public issue. Use GitHub's
[private vulnerability reporting](https://github.com/richarth/sanitiser/security/advisories/new) for this
repository.

Include enough detail to reproduce the issue: the affected package and version, your Umbraco and .NET
versions, and steps or a proof of concept. You can expect an acknowledgement within a few days.

## Scope

Sanitiser deletes or anonymises personal data on application startup, so the most sensitive concerns are:

- data being modified or deleted when it should not be — for example the environment guard, the
  `DomainsToExclude` filter, or the Super Admin protection not being honoured; and
- personal data remaining after a run that was expected to remove it — for example the cache-instruction
  cleanup silently failing.

Reports in these areas are especially valued.

## A note on transitive dependencies

Dependency scanners frequently flag transitive packages pulled in by Umbraco itself (for example `MailKit`,
`SQLitePCLRaw` or `Microsoft.Data.SqlClient`). These are governed by the Umbraco version your site references,
not by this library, and are best addressed by keeping Umbraco up to date. This repository audits its own
direct dependencies only (`NuGetAuditMode=direct`).
