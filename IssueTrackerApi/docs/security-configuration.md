# Security configuration

Set configuration outside the repository before running the API. Required settings:

| Setting | Environment variable | Development user-secret name |
| --- | --- | --- |
| SQL Server connection | `ConnectionStrings__Default` | `ConnectionStrings:Default` |
| JWT signing key | `Jwt__Key` | `Jwt:Key` |

`Jwt__Issuer` / `Jwt:Issuer` and `Jwt__Audience` / `Jwt:Audience` optionally override the non-secret defaults in `appsettings.json`.

In Visual Studio, use **Manage User Secrets** on the IssueTrackerApi project, then set the names above. ASP.NET Core loads user secrets in Development; use protected environment configuration in other environments. User secrets are stored outside the repository and are not encrypted. See [Microsoft's user secrets guidance](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-8.0).

Generate a unique signing key with a cryptographically secure generator (at least 32 random bytes, encoded as Base64). Never reuse a key from Git history. Startup rejects a missing SQL Server connection, a blank or short signing key (under 32 UTF-8 bytes), or a blank issuer/audience. Length validation cannot verify randomness. Configuration changes require an application restart.

Supply your own SQL Server connection. Do not disable certificate validation in production. Neither `.env` nor `appsettings.Local.json` is loaded automatically; use the environment or user secrets directly. Never place credentials in committed appsettings or launch profiles.

Public registration always creates a Developer. Admin is still required to create projects. Assign privileged roles only through a trusted administrative database process; there is no public role-assignment API. Review existing privileged accounts created before this fix. New registrations require a valid email and a password of at least eight characters.

Log in again after this update: bearer authentication now requires a positive user ID claim. Comments use that ID as their author, and issue history uses it as the status-change actor. Existing history rows are unchanged. Role changes do not revoke already issued tokens; tokens still expire after two hours, subject to the JWT validator's clock skew allowance.

## Historical exposure: reachable main remediated

The repository previously exposed a JWT signing key in public history. The value is intentionally withheld from this document. On 9 September 2026, `refs/heads/main` was replaced with verified rewritten history using a branch-specific `--force-with-lease` push. Normal fresh clones now expose only the cleaned branch history.

| Check | Result |
| --- | --- |
| Clean remote main | `82087250d75dc1d2a582e79d26ce8c38413e1a40` |
| Logical commit count | 3 |
| Old JWT matches in reachable cloned history | 0 |
| Historical tracked `bin/` files | 0 |
| Historical tracked `obj/` files | 0 |
| Git fsck on fresh clone | Passed |
| Forks, pull requests, tags, releases | 0 observed at remediation time |

Treat any previously exposed key as compromised. Removing the value from source and reachable history is not the same as rotation. Replace the key wherever it may have been used, restart all running instances, and verify old-key tokens are rejected. Review existing privileged accounts because historical registration accepted client-provided roles before the registration hardening.

A branch history rewrite cannot guarantee removal from unknown clones, local backups, browser caches, screenshots, search indexes, or internal GitHub cache. Keep the private backups and cleanup repository until cached-object remediation is complete and the new repository is stable. If needed, contact GitHub Support and request dereferencing or cache cleanup without including the secret value. Follow [GitHub's sensitive-data removal guidance](https://docs.github.com/en/authentication/keeping-your-account-and-data-secure/removing-sensitive-data-from-a-repository).

## Dependency audit: remediated in current source

`dotnet list IssueTrackerApi.sln package --vulnerable --include-transitive` completed on 9 September 2026 after patch upgrades and reported no vulnerable packages for `IssueTrackerApi` or `IssueTrackerApi.Tests` from the configured NuGet advisory sources.

| Area | Change |
| --- | --- |
| ASP.NET Core JWT bearer package | Updated from 8.0.8 to 8.0.31 |
| EF Core SQL Server and tooling | Updated from 8.0.8 to 8.0.31 |
| ASP.NET Core test host package | Updated from 8.0.8 to 8.0.31 |
| EF Core SQLite test provider | Updated from 8.0.8 to 8.0.31 |
| Local EF CLI tool | Updated from 8.0.8 to 8.0.31 |

The earlier advisory set included vulnerable transitive packages such as Azure.Identity, Microsoft.Extensions.Caching.Memory, Microsoft.Identity.Client, System.Formats.Asn1, System.Text.Json, and the SQLite native test dependency. The current scan no longer reports those advisories. This result depends on the NuGet advisory data available at audit time and should be rerun periodically.
