# Automated testing

## Run the suite

Use a .NET SDK capable of targeting .NET 8, with the .NET 8 and ASP.NET Core 8 runtimes installed. NuGet access is needed for the initial restore. Run these commands from the repository root:

```text
dotnet restore
dotnet build
dotnet test
```

The solution includes `IssueTrackerApi.Tests`, using xUnit v3 and the VSTest adapter. No SQL Server installation, database credentials, user secrets, Docker, or running API process is required.

To run one category or preserve machine-readable results:

```text
dotnet test --filter "Category=Unit"
dotnet test --filter "Category=Integration"
dotnet test --logger "trx;LogFileName=tests.trx" --results-directory TestResults
```

`TestResults`, `bin`, and `obj` are ignored. Report passed, failed, and skipped counts separately from the actual test output.

## Recorded local verification

Executed on 9 September 2026 on Windows, targeting .NET 8, with VSTest 17.14.1:

| Check | Observed result |
| --- | --- |
| `dotnet restore` | Succeeded, exit 0 |
| `dotnet build -c Release` | Succeeded, exit 0; 0 warnings, 0 errors |
| `dotnet test -c Release --no-build` | 55 passed, 0 failed, 0 skipped; exit 0 |
| Categories within that run | 3 unit cases and 52 integration cases, all executed |

These results were observed in command output; this verification did not create a new TRX report. The optional report command above can produce `TestResults/tests.trx`; any older report is not evidence of this run. These are local results, not a CI status or a guarantee about later changes. Parameterised theory rows count as individual cases; the workflow cases enumerate business-state transitions.

## Isolation and scope

`TokenServiceTests` exercises token generation directly. Integration tests use `WebApplicationFactory` and the real request pipeline, controllers, DTO validation, JWT bearer handler, role checks, BCrypt hashing, and EF Core persistence. Authentication is not replaced by a test handler.

Each integration test owns a fresh SQLite in-memory database and application host. The open SQLite connection keeps the database alive until the factory is disposed. A random signing key and synthetic user passwords are generated locally. The factory overrides configuration in the test host, without changing process environment variables or loading development user secrets. HTTP requests stay inside ASP.NET's test server.

SQLite replaces SQL Server only in the test project. `EnsureCreated` creates the test schema from the current EF model; the suite does not execute SQL Server migrations. This follows [EF Core's SQLite in-memory testing guidance](https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database#sqlite-in-memory).

## Behaviours covered

| Test group | Evidence |
| --- | --- |
| Issue workflow | Open -> InProgress -> Resolved -> Closed; persisted actor/timestamps and history; every other transition among the four states rejected; unknown states and missing issues; history scoped to its issue |
| Registration/login | Client cannot choose Admin, user ID, or password hash; per-user salted BCrypt hashes; duplicate registration preserves the existing account; invalid input rejected; eight-character password boundary; valid login grants API access; incorrect credentials rejected |
| JWT/authorisation | Signature, issuer, audience, lifetime, algorithm and user-ID checks; missing bearer token rejection; Admin project creation; public registration cannot gain Admin access |
| Server-controlled fields | Issue status, ID, assignment and timestamps protected from extra DTO fields; project identity/timestamp protected on creation; comment author derived from JWT; project and issue IDs preserved; assignment does not change the issue's project/status |
| Token service/configuration | Signed identity and role claims; two-hour expiry; nonpositive IDs rejected; startup rejects missing database configuration and invalid JWT settings |

## Limits and remaining work

- SQLite results do not establish SQL Server migration correctness, collation behaviour, query performance, or production connectivity. Duplicate-email coverage uses the same email spelling, not SQL Server-specific case rules.
- Existing project/issue/user IDs are tested on valid records. The current application has no foreign-key constraints or existence checks for these IDs. Rejection of orphan references and project membership/ownership controls are not claimed or asserted as implemented features.
- Concurrent status updates, concurrent duplicate registrations, token revocation, login throttling, deployments, and real TLS configuration are not covered. Some of these protections are not implemented.
- This suite does not review accounts that may have gained privileged roles before the registration hardening. See `security-configuration.md` before publication.
- Passing tests do not permanently establish dependency safety. The 9 September 2026 NuGet audit reported no vulnerable packages after .NET 8 patch updates, but dependency advisories should be checked again over time.
