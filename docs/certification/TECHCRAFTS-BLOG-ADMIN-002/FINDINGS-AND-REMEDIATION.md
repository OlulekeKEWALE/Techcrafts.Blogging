# TECHCRAFTS-BLOG-ADMIN-002 — Findings and Remediation

## Summary of Findings

| Finding ID | Severity | Description | Root Cause | Remediation & Fix | Verified Regression Test | Status |
|---|---|---|---|---|---|---|
| **F-01** | P1 | Markdown content rendered in `Detail.cshtml` allowed stored script execution via `Html.Raw`. | Missing HTML sanitization pipeline on markdown conversion. | Integrated `HtmlSanitizer` via `MarkdownRenderer.RenderToSafeHtml(content)` restricting allowed schemes to `http`, `https`, `mailto` and stripping executable tags/event handlers. | `E2E_19_SanitizedHtml_RendersSafelyWithoutExecutingScript` | **RESOLVED** |
| **F-02** | P1 | Concurrency race on unique slug insert against SQL Server unique index. | Application pre-check `AnyAsync` does not prevent database insert collision race under concurrent load. | Verified `SlugHelper.GenerateUniqueSlugAsync` deterministic suffix fallback and real SQL Server exception handling under concurrent insert. | `SQL_G06_UniqueSlugIndex_EnforcesUniquenessUnderConcurrentInserts` & `SQL_G06_SlugHelper_ResolvesCollisionsOnRealSqlServer` | **RESOLVED** |
| **F-03** | P2 | Telemetry background exporter crashed test process with `ObjectDisposedException` when host shuts down. | Unhandled `ObjectDisposedException` in `DatabaseTelemetryExporter.Export`. | Wrapped `_serviceProvider.CreateScope()` in try/catch block with graceful error handling and `ExportResult.Failure` on shutdown. | `EndToEndSecurityAndHttpPipelineTests` suite execution | **RESOLVED** |
| **F-04** | P2 | EF Core Multi-Provider conflict when running integration tests with WebApplicationFactory. | Two database providers (`SqlServer` and `InMemory`) simultaneously registered in DI during test initialization. | Configured `CustomWebApplicationFactory` to explicitly bind to dedicated test SQL Server database `Techcrafts_Certification_Test` with isolated cleanups. | `EndToEndSecurityAndHttpPipelineTests` | **RESOLVED** |
| **F-05** | P3 | Package downgrade warning `NU1605` on `Markdig` between Web (`1.3.2`) and Application (`1.5.1`). | Inconsistent direct PackageReference versions across project boundaries. | Upgraded `Techcrafts.Blogging.Web` package reference to `Markdig 1.5.1`. | Solution Build & Test Run in Release mode | **RESOLVED** |

## Unresolved Disclosures
- **Scheduled Publishing Daemon:** UI model preserves `PostStatus.Scheduled`, but no standing cron daemon is implemented. In accordance with P0 instructions, scheduling is surfaced as read-only state until background infrastructure is authorized.
