# TECHCRAFTS-BLOG-ADMIN-001 — Baseline Discovery and Root-Cause Assessment

## 1. Environment & Architecture Overview
- **Target Framework:** .NET 10.0 (`net10.0`)
- **Web Framework:** ASP.NET Core MVC with Razor Views
- **Architecture Pattern:** Clean Architecture with CQRS via MediatR (`Techcrafts.Blogging.Application`)
- **Persistence:** Entity Framework Core with SQL Server (`Microsoft.EntityFrameworkCore.SqlServer`), `BloggingDbContext` inheriting from `IdentityDbContext<IdentityUser>` and implementing `IBloggingDbContext`.
- **Identity & Auth:** ASP.NET Core Identity (`[Authorize(Roles = "Admin")]`), cookie authentication, external OAuth providers (Google, GitHub configured).
- **Starting Git Branch:** `master` (switched to `feature/admin-post-management` at commit `2f14c7b`).
- **Initial Working Tree State:** Uncommitted changes present (Telemetry migrations, SetupMiddleware, AdminController password management, Dockerfile deletion). These changes have been preserved intact.
- **Initial Test Suite:** Solution currently contains no automated test project (`0 test projects discovered`). Mandatory verification matrix tests must be implemented.

## 2. Confirmed Gaps and Root Causes

### Gap 1: No Administrative Post Listing
- **Symptom:** Admin has no interface or endpoint to view, search, filter, or paginate existing blog posts. Navigation layout `_AdminLayout.cshtml` has links for Overview, Draft Post, Analytics, and System Logs, but no "Blog Posts" / "Manage Posts".
- **Root Cause:** Missing `AdminPostsController` / action in `AdminController.cs`, missing CQRS query `GetAdminPostsQuery`, missing views, and missing navigation item.
- **Risk:** High. Inability to manage content lifecycle.

### Gap 2: Missing Post Edit Workflow
- **Symptom:** No `GET /admin/posts/{id}/edit` or `POST /admin/posts/{id}/edit` endpoint exists. No edit command or query exists.
- **Root Cause:** Only `CreatePostCommand` and `PublishPostCommand` were created in early scaffolding.
- **Risk:** High. Content cannot be corrected or updated without raw database intervention.

### Gap 3: Author Resolution Security Flaw
- **Symptom:** `Create.cshtml` binds `AuthorId` from `<input type="hidden" name="AuthorId" value="@Guid.NewGuid()" />`. The controller accepts the client-supplied `AuthorId` without authentication checks or resolution against the current logged-in user.
- **Root Cause:** Temporary placeholder in view and missing authenticated author resolution in handler/controller.
- **Risk:** High. Spoofed authorship or orphaned posts pointing to non-existent authors.

### Gap 4: Creation Always Auto-Publishes; Inactive UI Controls
- **Symptom:** In `AdminController.Create`, after executing `CreatePostCommand`, it unconditionally executes `await _mediator.Send(new PublishPostCommand(postId))` and redirects to public `PostsController.Index`. The checkboxes ("Publish immediately", "Send to Newsletter subscribers", "Generate with AI") are unbound or inactive.
- **Root Cause:** Hardcoded flow designed for initial testing.
- **Risk:** High. Editors cannot save drafts; all creations go live immediately.

### Gap 5: Inadequate Lifecycle & Idempotency Safeguards
- **Symptom:** `PublishPostCommandHandler` sets `PublishedDate = DateTimeOffset.UtcNow` on every call. Repeated publishing alters the original publication date. Unpublishing does not exist.
- **Root Cause:** Lack of state machine logic in `PublishPostCommandHandler` or missing unpublish/archive command.
- **Risk:** Medium-High. Breaks publication timeline, SEO timestamps, and RSS chronological feeds.

### Gap 6: Slug Collision Vulnerability
- **Symptom:** `CreatePostCommandHandler` generates a slug based on title slicing (`Regex.Replace(str, @"[^a-z0-9\s-]", "")`) but does not query for collision against existing posts. `PostConfiguration` enforces a unique index on `Slug`.
- **Root Cause:** No uniqueness suffix generation loop or duplicate slug handling.
- **Risk:** Medium. Post creation with duplicate titles throws an unhandled database unique constraint violation exception.

### Gap 7: Public Query Security Regarding Drafts
- **Symptom:** `GetPostBySlugQueryHandler` queries `_context.Posts.FirstOrDefaultAsync(p => p.Slug == request.Slug)`. It does **not** check whether `Status == PostStatus.Published`. Anyone knowing or guessing the slug can view drafts, scheduled, or unpublished posts at `/posts/{slug}`.
- **Root Cause:** Public detail endpoint omits post status filter.
- **Risk:** High. Premature or unauthorized leakage of unpublished editorial material.

### Gap 8: Concurrency & Audit Metadata
- **Symptom:** Multiple editors editing simultaneously can lead to lost updates without detection.
- **Root Cause:** EF Core `Post` configuration has no concurrency check / RowVersion.
- **Risk:** Medium. Overwriting concurrent edits.

### Gap 9: Deletion & Foreign Key Cascades
- **Symptom:** Posts have cascading relationships to `Comments`, `PostTags`, `PostReactions`, `PostClaps`, `Bookmarks`, `PageViews`. No administrative delete action or soft delete policy is defined.
- **Root Cause:** Absence of delete command and handler.
- **Risk:** High. Accidental deletion could cascade wipe user engagement data or fail due to FK constraints.

## 3. Schema & Database Impact Assessment
- The existing schema contains `PostStatus` (`Draft = 0`, `Published = 1`, `Scheduled = 2`, `Archived = 3`).
- In `Post`: `CreatedDate`, `UpdatedDate`, `CreatedBy`, `UpdatedBy` exist as part of `IAuditableEntity`.
- Concurrency token: A byte array `RowVersion` / concurrency token or `UpdatedDate` concurrency mapping can be introduced or verified.
- Existing migrations: `20260724220607_InitialCreate`, `20260724230023_AddIdentitySchema`, `20260725002130_AddPostReactions`, `20260728015404_AddTelemetryLogs`.
- Historical migrations will NOT be modified. If schema adjustments are required, a new additive EF Core migration will be introduced.

## 4. Acceptance Criteria & Test Strategy
- Dedicated, fully authorized administrative post listing (`/admin/posts`) supporting filter by status, search, and pagination.
- Full editing capability (`/admin/posts/{id}/edit`) with pre-populated form, Markdown preview, and distinct actions ("Save Draft", "Save Changes", "Publish", "Unpublish", "Delete/Archive").
- Enforced server-side author resolution from `ClaimsPrincipal`.
- Unique slug generation with collision resilience.
- Public route `/posts/{slug}` rejects non-published posts unless accessed in authorized admin preview mode.
- Comprehensive automated test suite covering unit, integration, and security scenarios (ADM-01 through ADM-26).
