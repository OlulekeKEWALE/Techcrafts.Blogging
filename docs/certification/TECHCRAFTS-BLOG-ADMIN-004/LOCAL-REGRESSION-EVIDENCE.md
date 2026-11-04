# TECHCRAFTS-BLOG-ADMIN-004 — Local Regression & Concurrency Evidence

## 1. Local Build & Test Summary (Release Configuration)

The complete solution was restored, compiled, and executed in **Release** configuration using .NET SDK 10.0.402:

```shell
dotnet restore Techcrafts.Blogging.slnx
dotnet build Techcrafts.Blogging.slnx -c Release --no-restore
dotnet test Techcrafts.Blogging.Tests/Techcrafts.Blogging.Tests.csproj -c Release --no-build --logger "trx;LogFileName=ReleaseTestResults.trx"
```

### Execution Metrics:
- **Build Outcome:** Succeeded with 0 errors (13 NuGet vulnerability warnings from transitive package versions).
- **Test Results File:** `Techcrafts.Blogging.Tests/TestResults/ReleaseTestResults.trx`
- **Total Test Cases Executed:** **40**
- **Passed:** **40**
- **Failed:** **0**
- **Skipped:** **0**
- **Pass Rate:** **100%**
- **Full Run Duration:** **3 minutes 58 seconds**

---

## 2. Test Breakdown by Domain

| Test Suite Class | Description | Executed | Passed | Failed |
|---|---|:---:|:---:|:---:|
| `PlaywrightBrowserE2ETests` | Real Chromium browser tests against out-of-process Kestrel host | 9 | 9 | 0 |
| `SqlServerConcurrencyIntegrationTests` | SQL Server 2022 rowversion, stale rejection, and second-race conflict tests | 3 | 3 | 0 |
| `SqlServerIntegrationCertificationTests` | SQL Server unique slug index, audit tracking, and atomic cascade deletions | 5 | 5 | 0 |
| `AdminPostManagementTests` | CQRS Command/Query unit and in-memory integration tests | 13 | 13 | 0 |
| `EndToEndSecurityAndHttpPipelineTests` | RBAC, antiforgery, HTML sanitization, and public draft 404 tests | 6 | 6 | 0 |
| `AdminControllerSecurityTests` | Security attribute reflection tests | 4 | 4 | 0 |
| **Total** | | **40** | **40** | **0** |

---

## 3. Second-Race Concurrency Verification
The newly introduced scenario `SQL_C04_SecondRaceConflict_AdminCOverwritesWhileAdminBViewsConflictScreen_AdminBMustConflictAgain` confirmed:
1. Version V1 created in SQL Server.
2. Admin A updates to V2 (success).
3. Admin B attempts save with stale V1 -> Rejected with `ConcurrencyConflict`, receiving V2 token.
4. While Admin B views conflict screen, Admin C updates to V3 (success).
5. Admin B submits overwrite with V2 token -> **Rejected with `ConcurrencyConflict`**. Admin B cannot overwrite Admin C's changes silently.
6. Admin B receives updated V3 state and refreshed V3 token.
