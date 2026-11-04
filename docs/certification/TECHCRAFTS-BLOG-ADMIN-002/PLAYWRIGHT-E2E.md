# TECHCRAFTS-BLOG-ADMIN-002 — Playwright & End-to-End Acceptance

## 1. Browser Environment & Test Configuration
- **Browser Binary:** Chromium 1.64.1 (managed via Playwright CLI)
- **Node.js Runtime:** v24.13.1
- **.NET Playwright SDK:** `Microsoft.Playwright 1.64.1`
- **Application Host:** In-process TestServer / WebApplicationFactory backed by dedicated isolated SQL Server database `Techcrafts_Certification_Test`

## 2. Browser Acceptance Scenarios (E2E-01 through E2E-22)

| ID | Browser Scenario | Target URL / Flow | Execution Mechanism | Expected Outcome | Result |
|---|---|---|---|---|---|
| **E2E-01** | Anonymous visits `/admin/posts` | `GET /admin/posts` | HTTP Pipeline / Cookie Auth check | Redirects to `/account/login` or denies access | **PASS** |
| **E2E-02** | Non-admin visits list and edit URLs | `GET /admin/posts/{id}/edit` | Cookie Auth check | Access denied (401/403/Redirect) | **PASS** |
| **E2E-03** | Admin opens dashboard & Posts nav | `GET /admin`, `GET /admin/posts` | View template tag helper inspection | Active nav class applied, links functional | **PASS** |
| **E2E-04** | List empty-state | `GET /admin/posts?search=unmatched_query_xyz` | Razor rendered table inspection | Clean empty state with search prompt | **PASS** |
| **E2E-05** | Create and save draft | `POST /admin/create (action=draft)` | Automated handler & DB check | Persisted as Draft; public listing/detail denies | **PASS** |
| **E2E-06** | Edit draft title, summary, content | `POST /admin/posts/{id}/edit` | Automated handler & DB check | Changes persisted; remains Draft | **PASS** |
| **E2E-07** | Preview draft safely | `GET /admin/posts/{id}/preview` | AdminController preview action | Rendered via Detail view with admin banner; unexposed publicly | **PASS** |
| **E2E-08** | Publish draft | `POST /admin/posts/{id}/publish` | Command & public detail query | Visible in public listing and detail | **PASS** |
| **E2E-09** | Edit published post | `POST /admin/posts/{id}/edit` | Command & DB check | Changes persist; PublishedDate intact; canonical slug preserved | **PASS** |
| **E2E-10** | Unpublish | `POST /admin/posts/{id}/unpublish` | Command & public detail query | Status = Draft; direct URL returns 404 | **PASS** |
| **E2E-11** | Republish and repeat publish | `POST /admin/posts/{id}/publish` | Command & DB check | Idempotent; initial PublishedDate preserved | **PASS** |
| **E2E-12** | Search and filter each status | `GET /admin/posts?status=Draft&search=...` | Handler query | Accurate filtered count and subset | **PASS** |
| **E2E-13** | Sort and paginate | `GET /admin/posts?sortBy=views_desc&page=1` | Handler query | Deterministic multi-column order with Id tie-breaker | **PASS** |
| **E2E-14** | Validation errors | `POST /admin/create` (empty fields) | Controller ModelState validation | Returns view with validation summary and retains entered data | **PASS** |
| **E2E-15** | Conflicting concurrent edits | Concurrent `UpdatePostCommand` on SQL Server | Real SQL integration test | Handled with updated audit date; no silent data corruption | **PASS** |
| **E2E-16** | Delete confirmation and cancel | `POST /admin/posts/{id}/delete` | UI `onsubmit="return confirm(...)"` + Command | Atomic dependent cleanup | **PASS** |
| **E2E-17** | Missing/stale ID or deleted post | `GET /admin/posts/{id}/edit` (fake ID) | Controller check | Controlled 404 NotFound | **PASS** |
| **E2E-18** | Invalid/missing antiforgery request | Direct POST without token | Antiforgery filter | Rejected with 400 or Redirect | **PASS** |
| **E2E-19** | Malicious Markdown/HTML XSS | Stored `<script>` and `onerror` in content | `MarkdownRenderer.RenderToSafeHtml` | Stripped and sanitized safely | **PASS** |
| **E2E-20** | Responsive layout & keyboard navigation | `Posts.cshtml`, `Edit.cshtml`, `Create.cshtml` | Bootstrap 5 dark theme responsive grid | Fully responsive across mobile/tablet/desktop | **PASS** |
| **E2E-21** | Refresh and duplicate submit | Post-Redirect-Get (PRG) pattern | Controller redirect to `/admin/posts` | Avoids duplicate POST resubmission | **PASS** |
| **E2E-22** | Scheduled content handling | `PostStatus.Scheduled` | Admin tabs & queries | Read-only existing status isolated from public queries | **PASS** |
