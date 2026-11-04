# TECHCRAFTS-BLOG-ADMIN-004 — Source Diff Quality & Security Audit

## 1. Architectural Boundary Audit

The complete diff against `origin/master` was inspected to confirm strict adherence to Clean Architecture principles:

### 1.1 Domain Layer (`Techcrafts.Blogging.Domain`)
- **Entity Modifications:** [`Post.cs`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Domain/Entities/Post.cs) was augmented with `byte[]? RowVersion` to support optimistic concurrency tokens.
- **Purity:** Zero framework or UI dependencies introduced into the domain.

### 1.2 Application Layer (`Techcrafts.Blogging.Application`)
- **CQRS Commands:**
  - `CreatePostCommand` / `CreatePostCommandHandler`: Generates unique collision-resistant slugs, sets initial reading time, creates post with author server-mapping.
  - `UpdatePostCommand` / `UpdatePostCommandHandler`: Carries client's `RowVersion`, attaches original token to EF change tracker, catches `DbUpdateConcurrencyException`, reloads uncommitted conflict state, and returns `UpdatePostResult.ConcurrencyConflict`.
  - `DeletePostCommand` / `DeletePostCommandHandler`: Cleans dependent reactions, claps, bookmarks, page views, and embeddings before cascading deletion.
  - `PublishPostCommand` / `PublishPostCommandHandler`: Sets status to `Published` while idempotently preserving original `PublishedDate`.
  - `UnpublishPostCommand` / `UnpublishPostCommandHandler`: Transitions post to `Draft`.
- **CQRS Queries:**
  - `GetAdminPostsQuery` / `GetAdminPostsQueryHandler`: Dedicated administrative post listing query with status filtering, keyword search, and pagination (never repurposing the public published-only query).
  - `GetAdminPostByIdQuery` / `GetAdminPostByIdQueryHandler`: Fetches administrative post details with current `RowVersion` token.

### 1.3 Infrastructure Layer (`Techcrafts.Blogging.Infrastructure`)
- **EF Core Mapping:** In `PostConfiguration.cs`, `builder.Property(p => p.RowVersion).IsRowVersion()` maps the token to SQL Server native `rowversion` (`timestamp`).
- **Migrations:** Migration `20261103232610_AddPostRowVersion` adds `RowVersion rowversion NULL` non-destructively, preserving existing records and seeding monotonic 8-byte tokens.

### 1.4 Web / Presentation Layer (`Techcrafts.Blogging.Web`)
- **Authorization:** `AdminController` is decorated with `[Authorize(Roles = "Admin")]` at class level.
- **CSRF Protection:** All POST actions (`Create`, `Edit`, `Publish`, `Unpublish`, `Delete`) enforce `[ValidateAntiForgeryToken]`.
- **Server Identity Resolution:** `EnsureAuthorIdForCurrentUserAsync` resolves current authenticated ASP.NET Core Identity user to an `Author` record server-side; client cannot spoof `AuthorId`.
- **HTTP 409 Conflict Handling:** Concurrency clashes emit HTTP 409 and render `Conflict.cshtml` displaying unsaved edits side-by-side with persisted database values.

---

## 2. Threat Modeling & Security Review Matrix

| Security Domain | Risk Assessment | Mitigation Applied | Verification Status |
|---|---|---|:---:|
| **Authentication & RBAC** | Unauthorized users accessing post management | Class-level `[Authorize(Roles = "Admin")]` on `AdminController`; anonymous requests return HTTP 302 to `/account/login`. | **PASS** |
| **CSRF / Antiforgery** | Cross-site forged post creation, modification, or deletion | `[ValidateAntiForgeryToken]` on all POST actions; validated via `EndToEndSecurityAndHttpPipelineTests`. | **PASS** |
| **Stored XSS** | Malicious script payloads injected via Markdown authoring | Markdig HTML sanitization active on rendering pipeline; validated with script and `onerror` test payloads. | **PASS** |
| **Mass Assignment** | Malicious users binding arbitrary entity properties | Specific DTO parameters bound via `[FromForm]`; no entity models bound directly from HTTP requests. | **PASS** |
| **Draft Post Disclosure** | Unauthenticated visitors reading unpublished drafts | Public `GET /posts/{slug}` strictly filters by `PostStatus.Published`; drafts return HTTP 404 Not Found. | **PASS** |
| **Concurrent Lost Updates** | Two administrators overwriting edits simultaneously | SQL Server `rowversion` token enforced via EF Core `IsRowVersion()`; second committer receives HTTP 409 Conflict. | **PASS** |
| **Second-Race Overwrite** | Admin B overwriting while Admin C commits | Admin B's overwrite with token V2 is rejected when Admin C has incremented row to V3; verified by `SQL_C04_SecondRaceConflict`. | **PASS** |
