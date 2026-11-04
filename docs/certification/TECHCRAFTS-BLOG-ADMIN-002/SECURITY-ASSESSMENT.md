# TECHCRAFTS-BLOG-ADMIN-002 — Security Assessment

## 1. Threat Scenarios & Testing Evidence

### A. Authentication & Privilege Bypass (ADM-01, ADM-02)
- **Control:** `[Authorize(Roles = "Admin")]` at controller class level on `AdminController`.
- **Negative Test:** Anonymous requests to `/admin/posts` and `/admin/create` return HTTP 302 Redirect to `/account/login`.
- **Test:** `AdminControllerSecurityTests.ADM_01_ADM_02_AdminController_EnforcesAuthorizeAdminAttribute`, `EndToEndSecurityAndHttpPipelineTests.E2E_01_AnonymousVisitsAdminPosts_RedirectsToLogin`.
- **Outcome:** **PASS**

### B. Cross-Site Request Forgery (CSRF / ADM-16)
- **Control:** `[ValidateAntiForgeryToken]` on all mutating endpoints (`Create`, `Edit`, `Publish`, `Unpublish`, `Delete`, `ChangePassword`). Form templates include `@Html.AntiForgeryToken()`.
- **Negative Test:** Unauthenticated or tokenless POST requests to `/admin/posts/{id}/unpublish` fail immediately with HTTP 400 Bad Request or HTTP 302 Redirect.
- **Test:** `AdminControllerSecurityTests.ADM_16_MutatingActions_HaveValidateAntiForgeryToken`, `EndToEndSecurityAndHttpPipelineTests.E2E_18_MutationsWithoutCsrfToken_AreRejected`.
- **Outcome:** **PASS**

### C. Insecure Direct Object References (IDOR) & Mass Assignment (ADM-13)
- **Control:** `AuthorId` is resolved exclusively via `EnsureAuthorIdForCurrentUserAsync` using `User.FindFirstValue(ClaimTypes.Email)`.
- **Negative Test:** Form payload containing arbitrary or forged `AuthorId` GUID is completely ignored.
- **Test:** `AdminControllerSecurityTests.ADM_13_ForgedAuthorIdIgnored_AuthorResolvedFromAuthenticatedPrincipal`.
- **Outcome:** **PASS**

### D. Stored & Reflected Cross-Site Scripting (XSS / ADM-17)
- **Control:** `MarkdownRenderer.RenderToSafeHtml` uses `HtmlSanitizer` with strict allowed schemes (`http`, `https`, `mailto`), stripping `<script>`, event handlers (`onerror`, `onload`, `onclick`), and `javascript:` URLs.
- **Negative Test:** Stored payload `Here is safe text. <script>alert('pwned')</script> <img src=x onerror=alert(1)> [click me](javascript:alert(1))` rendered via `/posts/{slug}` strip the script, onerror attribute, and javascript pseudo-protocol while preserving safe text.
- **Test:** `EndToEndSecurityAndHttpPipelineTests.E2E_19_SanitizedHtml_RendersSafelyWithoutExecutingScript`.
- **Outcome:** **PASS**

### E. Public Draft Content Isolation (ADM-08, ADM-10)
- **Control:** `GetPostBySlugQueryHandler` explicitly filters `p.Status == PostStatus.Published` unless `AllowDraft = true` is supplied by authorized admin preview.
- **Negative Test:** Public request to `/posts/{draft-slug}` returns HTTP 404 NotFound.
- **Test:** `EndToEndSecurityAndHttpPipelineTests.E2E_05_PublicPostDetail_DoesNotDiscloseDraftPost`, `AdminPostManagementTests.ADM_10_AdminUnpublishesPost_PublicListingAndDirectUrlDenyIt`.
- **Outcome:** **PASS**
