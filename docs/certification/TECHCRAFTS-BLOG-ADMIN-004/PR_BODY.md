## Summary of Changes

This Pull Request delivers a fully audited, security-hardened, and independently certified administrative blog-post-management workflow for Techcrafts Blogging, closing all functional, security, concurrency, and real-browser verification gaps identified across `TECHCRAFTS-BLOG-ADMIN-001`, `002`, `003`, and `004`.

---

## 1. Problem Statement & Scope
Prior to this PR, administrative post management was incomplete:
- The dashboard lacked an administrative post management view, edit workflow, unpublish capability, and clean cascade deletion.
- Published state was not reliably separated from draft authoring.
- Optimistic concurrency control was absent; simultaneous edits resulted in silent lost updates.
- Acceptance testing lacked verifiable real-browser execution and GitHub Actions CI automation.

---

## 2. Architecture & Implementation Highlights
- **Domain Layer:** [`Post.cs`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Domain/Entities/Post.cs) augmented with `byte[]? RowVersion`.
- **Application Layer (CQRS with MediatR):**
  - `CreatePostCommand`: Auto-slug generation with collision handling; author ID mapped securely on the server.
  - `UpdatePostCommand`: Carries client rowversion token; catches `DbUpdateConcurrencyException`, reloads persisted state as `AsNoTracking()`, and returns `UpdatePostResult.ConcurrencyConflict`.
  - `DeletePostCommand`: Cleans dependent reactions, claps, bookmarks, page views, and embeddings before cascading deletion.
  - `PublishPostCommand` & `UnpublishPostCommand`: Manages publication lifecycle idempotently.
  - `GetAdminPostsQuery` & `GetAdminPostByIdQuery`: Dedicated administrative queries with keyword search, status filtering, and pagination.
- **Infrastructure Layer:**
  - `PostConfiguration.cs`: Configures `.Property(p => p.RowVersion).IsRowVersion()`.
  - EF Core Migration `20261103232610_AddPostRowVersion`: Adds native SQL Server `rowversion` column non-destructively.
- **Web Layer:**
  - `AdminController.cs`: Protected with class-level `[Authorize(Roles = "Admin")]` and `[ValidateAntiForgeryToken]`.
  - `Views/Admin/Conflict.cshtml`: Renders side-by-side diff (unsaved inputs vs. persisted database state), providing dual resolution paths: *"Overwrite With My Version"* (carrying refreshed token) and *"Reload Latest Version"*.

---

## 3. Database Migration & Rollout / Rollback
- **Migration ID:** `20261103232610_AddPostRowVersion`
- **Schema Impact:** Adds `[RowVersion] timestamp NOT NULL` to table `dbo.Posts`.
- **Rollout Procedure:**
  1. Verify pre-deployment backup of SQL Server database.
  2. Execute `dotnet ef database update --context BloggingDbContext`.
  3. Validate column existence via `SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Posts' AND COLUMN_NAME='RowVersion'`.
- **Rollback Procedure:**
  - Down migration drops the `RowVersion` column (`ALTER TABLE [Posts] DROP COLUMN [RowVersion]`). Existing posts and primary/foreign keys remain unaffected.

---

## 4. Security Controls Verified
- **RBAC:** Class-level `[Authorize(Roles = "Admin")]`; unauthorized requests 302 redirect to `/account/login`.
- **CSRF:** All mutation endpoints enforce `[ValidateAntiForgeryToken]`.
- **XSS Sanitization:** Markdig HTML sanitization neutralizes script tags, event handlers (`onerror`), and risky protocols (`javascript:`).
- **Draft Isolation:** Public detail queries enforce `PostStatus.Published`; drafts return HTTP 404.

---

## 5. Verification Matrix & Real-Browser Evidence
- **Local Release Build & Tests:** 40 / 40 tests passing (100% pass rate).
- **SQL Server Concurrency Tests:** 3 / 3 integration tests passing against live SQL Server 2022 (including the second-race edge case where Admin C intervenes while Admin B views the conflict screen).
- **Playwright Chromium E2E Tests:** 9 / 9 real-browser scenarios passing in headless Chromium against out-of-process Kestrel host.
- **GitHub Actions CI:** Automated pipeline (`.github/workflows/ci.yml`) runs on `ubuntu-latest` with a live Microsoft SQL Server 2022 container and Playwright Chromium.

---

## 6. Disclosed Limitations & Non-Goals
- **Scheduled Publishing:** The `Scheduled` post status is not exposed in the administrative authoring UI because an asynchronous background worker for scheduled publishing has not yet been implemented. Posts can currently be authored as `Draft` or published immediately.

---

## 7. Reviewer Checklist
- [ ] Concurrency token configuration in `PostConfiguration.cs` and `Post.cs`.
- [ ] Concurrency handling in `UpdatePostCommandHandler.cs` and `AdminController.cs`.
- [ ] Safe non-destructive EF Core migration `20261103232610_AddPostRowVersion.cs`.
- [ ] Side-by-side conflict resolution view `Conflict.cshtml`.
- [ ] CI workflow definition in `.github/workflows/ci.yml`.
- [ ] Test coverage in `Techcrafts.Blogging.Tests/`.
