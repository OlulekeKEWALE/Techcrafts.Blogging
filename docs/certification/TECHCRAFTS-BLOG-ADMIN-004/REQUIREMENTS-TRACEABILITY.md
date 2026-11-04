# TECHCRAFTS-BLOG-ADMIN-004 — Requirements Traceability Matrix

## Traceability from ADMIN-001 through ADMIN-004

| Requirement Code | Description | Originating Phase | Implementation Location | Verification Evidence |
|---|---|---|---|---|
| **REQ-ADM-01** | Admin Post Listing View | ADMIN-001 | [`AdminController.cs`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Web/Controllers/AdminController.cs)<br>[`Posts.cshtml`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Web/Views/Admin/Posts.cshtml) | `E2E_03_AdminPostsTable_RendersColumnsAndNavigation` |
| **REQ-ADM-02** | Dedicated Admin Query (Not Public Filter) | ADMIN-001 | [`GetAdminPostsQuery.cs`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Application/Features/Posts/Queries/GetAdminPostsQuery.cs) | `AdminPostManagementTests.cs` (Handle_ReturnsPaginatedPosts) |
| **REQ-ADM-03** | Server-Side Author Resolution | ADMIN-001 | `AdminController.EnsureAuthorIdForCurrentUserAsync` | `E2E_05_CreateDraftPost_PersistsAndShowsInListing` |
| **REQ-ADM-04** | Unique Slug Generation & Collision Handling | ADMIN-001 | [`SlugHelper.cs`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Application/Common/Utils/SlugHelper.cs)<br>`CreatePostCommandHandler.cs` | `SQL_G06_UniqueSlugIndex_EnforcesUniquenessUnderConcurrentInserts` |
| **REQ-ADM-05** | Post Edit & Slug Preservation | ADMIN-001 | `UpdatePostCommandHandler.cs`<br>[`Edit.cshtml`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Web/Views/Admin/Edit.cshtml) | `E2E_06_EditPost_UpdatesContentAndPreservesSlug` |
| **REQ-ADM-06** | Editorial Publish & Unpublish Lifecycle | ADMIN-001 | `PublishPostCommandHandler.cs`<br>`UnpublishPostCommandHandler.cs` | `E2E_08_PublishAndUnpublish_LifecycleInRealBrowser` |
| **REQ-ADM-07** | Clean Cascade Post Deletion | ADMIN-001 | `DeletePostCommandHandler.cs` | `SQL_G09_AtomicDeleteWithCascadeAndDependentCleanups_OnRealSqlServer`<br>`E2E_09_DeletePost_RemovesPostCleanly` |
| **REQ-ADM-08** | RBAC Enforcement (`[Authorize(Roles="Admin")]`) | ADMIN-002 | `AdminController.cs` | `AdminControllerSecurityTests.cs`<br>`E2E_01_AnonymousUser_RedirectedToLogin` |
| **REQ-ADM-09** | CSRF / Antiforgery Tokens | ADMIN-002 | `AdminController.cs` (`[ValidateAntiForgeryToken]`) | `E2E_03_MutationsWithoutAntiforgeryToken_RejectedWithBadRequestOrRedirect` |
| **REQ-ADM-10** | Stored XSS Neutralization | ADMIN-002 | `MarkdownRenderer.cs` | `E2E_19_SanitizedHtml_RendersSafelyWithoutExecutingScript` |
| **REQ-ADM-11** | Public Draft Isolation (HTTP 404) | ADMIN-002 | `GetPostBySlugQueryHandler.cs` | `E2E_05_PublicPostDetail_DoesNotDiscloseDraftPost` |
| **REQ-ADM-12** | SQL Server `rowversion` Concurrency Token | ADMIN-003 | `Post.cs`<br>`PostConfiguration.cs` | `20261103232610_AddPostRowVersion.cs` |
| **REQ-ADM-13** | Concurrency Conflict Handling (HTTP 409) | ADMIN-003 | `UpdatePostCommandHandler.cs`<br>`AdminController.cs` | `SQL_C02_C03_ConcurrentEdit_RejectsStaleUpdateWithConcurrencyConflict` |
| **REQ-ADM-14** | Side-by-Side Diff & Resolution Options | ADMIN-003 | [`Conflict.cshtml`](file:///E:/TechCrafts/Open%20Sources/Blogging/Techcrafts.Blogging.Web/Views/Admin/Conflict.cshtml) | `E2E_07_E2E_15_OptimisticConcurrencyConflict_DetectedAndResolvedInRealBrowser` |
| **REQ-ADM-15** | Real-Browser Playwright Acceptance | ADMIN-003 | `PlaywrightBrowserE2ETests.cs` | 9 / 9 Chromium tests passing; 10 PNG screenshots |
| **REQ-ADM-16** | Second-Race Concurrency Edge Case | ADMIN-004 | `UpdatePostCommandHandler.cs` | `SQL_C04_SecondRaceConflict_AdminCOverwritesWhileAdminBViewsConflictScreen_AdminBMustConflictAgain` |
| **REQ-ADM-17** | GitHub PR Publication & Tracking | ADMIN-004 | Branch `feature/admin-post-management` | PR #1: `https://github.com/OlulekeKEWALE/Techcrafts.Blogging/pull/1` |
| **REQ-ADM-18** | GitHub Actions Automated CI Pipeline | ADMIN-004 | `.github/workflows/ci.yml` | Workflow runs 41753126508 and 41753646176 |
