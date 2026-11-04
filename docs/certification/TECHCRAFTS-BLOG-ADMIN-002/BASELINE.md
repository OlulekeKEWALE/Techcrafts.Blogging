# TECHCRAFTS-BLOG-ADMIN-002 — Baseline Discovery & Traceability

## 1. Local Git & Environment Provenance
- **Repository Path:** `E:\TechCrafts\Open Sources\Blogging`
- **Current Branch:** `feature/admin-post-management`
- **Current HEAD SHA:** `2f14c7bba743e2dbb2916a6fe2f618e29d1acda2`
- **Default Upstream Branch:** `master`
- **Remote URL:** `https://github.com/OlulekeKEWALE/Techcrafts.Blogging.git`
- **Branch Existence Status:** 
  - `feature/admin-post-management` exists locally.
  - Remote only contains `master` (`remotes/origin/master` at `2f14c7b`).
  - No remote pushes or PR creations have occurred (zero unapproved remote mutations).
- **Working Tree State:** Prior uncommitted files from local development (Telemetry entity & migrations, SetupMiddleware, AdminController password management, Dockerfile deletion) have been preserved without modification.

## 2. Prior Deliverables Reconciliation (TECHCRAFTS-BLOG-ADMIN-001)
- `docs/admin/TECHCRAFTS-BLOG-ADMIN-001-BASELINE.md` — Verified present.
- `docs/admin/TECHCRAFTS-BLOG-ADMIN-001-IMPLEMENTATION.md` — Verified present.
- `docs/admin/TECHCRAFTS-BLOG-ADMIN-001-CERTIFICATION.md` — Verified present.
- Prior claim of 17 passing tests: Independently reproduced and verified. The 17 unit tests covered 26 requirement IDs (as requirement scenarios mapped onto 17 test methods).

## 3. Discovered Vulnerabilities & Pre-Remediation Failures
During Phase A & Phase C discovery, the following gaps in the initial baseline were uncovered:
1. **Raw Markdown XSS Risk:** `Techcrafts.Blogging.Web/Views/Posts/Detail.cshtml` invoked `Html.Raw(Markdig.Markdown.ToHtml(Model.Content))` directly without an HTML sanitization boundary. User-supplied HTML/JavaScript tags in markdown could execute in browsers.
2. **Missing Real SQL Server Certification:** Baseline verified queries against EF Core InMemory provider; real SQL Server constraints (foreign key cascades, concurrent duplicate slug insertion races, and datetimeoffset index performance) were unverified.
3. **Telemetry Exporter Disposal Crash in Test Host:** `DatabaseTelemetryExporter` could attempt to resolve scoped DbContext during test host shutdown, producing an unhandled `ObjectDisposedException`.

All three defects have been remedied and verified with new tests.
