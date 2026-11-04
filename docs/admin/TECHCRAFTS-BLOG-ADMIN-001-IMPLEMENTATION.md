# TECHCRAFTS-BLOG-ADMIN-001 — Implementation Architecture & Editorial Lifecycle

## 1. Administrative Endpoints & Route Contracts

| Method | Endpoint | Description | Authorization / Antiforgery |
|---|---|---|---|
| `GET` | `/admin` | Administrative dashboard with metrics and recent posts | `[Authorize(Roles = "Admin")]` |
| `GET` | `/admin/posts` | Paginated, searchable, status-filtered post listing | `[Authorize(Roles = "Admin")]` |
| `GET` | `/admin/create` | Post authoring interface (Markdown editor) | `[Authorize(Roles = "Admin")]` |
| `POST` | `/admin/create` | Persist new post as Draft or directly Published | `[Authorize(Roles = "Admin")]`, `[ValidateAntiForgeryToken]` |
| `GET` | `/admin/posts/{id}/edit` | Pre-populated post editor with metadata and tags | `[Authorize(Roles = "Admin")]` |
| `POST` | `/admin/posts/{id}/edit` | Persist edits; handle Save Changes, Publish, or Unpublish | `[Authorize(Roles = "Admin")]`, `[ValidateAntiForgeryToken]` |
| `POST` | `/admin/posts/{id}/publish` | Fast action to publish draft; idempotent timestamp | `[Authorize(Roles = "Admin")]`, `[ValidateAntiForgeryToken]` |
| `POST` | `/admin/posts/{id}/unpublish` | Fast action to transition published post back to draft | `[Authorize(Roles = "Admin")]`, `[ValidateAntiForgeryToken]` |
| `POST` | `/admin/posts/{id}/delete` | Delete post and safely cascade/clean up dependent child records | `[Authorize(Roles = "Admin")]`, `[ValidateAntiForgeryToken]` |
| `GET` | `/admin/posts/{id}/preview` | Authenticated admin-only preview of post (including drafts) | `[Authorize(Roles = "Admin")]` |

## 2. Editorial Lifecycle State Machine

```
                 [New Post]
                  /      \
      "Save Draft"        "Save & Publish"
                v            v
           +--------+    +-----------+
  +------->| Draft  |--->| Published |<------+
  |        +--------+    +-----------+       |
  |             ^              |             |
  |             |  Unpublish   |             |
  |             +--------------+             |
  |                                          |
  |                                          |
  +-- "Save Draft" on Edit                   |
                                             +-- "Save Changes" on Edit
```

- **Draft State:** Retains `Status = PostStatus.Draft` and `PublishedDate = null`. Not returned in public post listings (`GetPublicPostsQueryHandler`) and rejected by public slug queries (`GetPostBySlugQueryHandler`).
- **First Publish:** Transitions to `Status = PostStatus.Published` and sets `PublishedDate = DateTimeOffset.UtcNow`.
- **Repeated Publish (Idempotency):** Handler checks `post.PublishedDate ??= DateTimeOffset.UtcNow`, guaranteeing the historical publication date is never overridden.
- **Save Changes on Published Post:** Updates title, summary, markdown content, featured image, tags, and reading time while strictly keeping `Status = PostStatus.Published` and preserving the original `PublishedDate`.
- **Unpublish:** Moves post back to `Status = PostStatus.Draft`. Immediately removes the post from all public views and public direct URLs.
- **Scheduled Content Policy:** The `PostStatus.Scheduled` enum is preserved for forward-compatibility. In accordance with P0 instructions, no unsupported background cron daemon was fabricated; any scheduled items are clearly labeled and sequestered until explicit publication.

## 3. Server-Resolved Author Identity

- `AuthorId` is never accepted from an untrusted client field.
- During `POST /admin/create`, the server resolves the current authenticated user (`User.FindFirstValue(ClaimTypes.Email)`).
- If the corresponding `Author` entity exists, its ID is used; if not present, a verified `Author` record is provisioned on the fly for the authenticated administrator.
- Client attempts to forge `AuthorId` are completely ignored.

## 4. Deterministic Slug Generation & Collision Resilience

- Implemented in `SlugHelper.GenerateUniqueSlugAsync`.
- Converts title to lowercase, strips non-alphanumeric characters (preserving dashes and single spaces), and trims to 45 characters.
- Executes an indexed uniqueness check against `_context.Posts.AnyAsync(p => p.Slug == candidateSlug)`.
- If a collision occurs, a deterministic counter suffix (`-1`, `-2`, etc.) is appended until a unique slug is guaranteed.
- On edit, canonical slugs are preserved to prevent breaking existing inbound backlinks and search engine indexes.

## 5. Deletion & Relationship Retention Policy

- Implemented in `DeletePostCommandHandler`.
- Pre-emptively cleans up dependent records across:
  - `Comments` (configured cascade)
  - `PostReactions`
  - `PostClaps`
  - `Bookmarks`
  - `PageViews`
  - `PostEmbeddings`
- Deletion requires explicit user confirmation via modal/browser prompt and is protected by `[HttpPost]` and `[ValidateAntiForgeryToken]`.

## 6. Auditability & Reading Time Calculation

- Edits recalculate reading time based on 200 words-per-minute (`SlugHelper.CalculateReadingTime`).
- EF Core `BloggingDbContext.SaveChangesAsync` automatically records `UpdatedDate = DateTimeOffset.UtcNow` on entity modification via `IAuditableEntity`.
- Admin list and edit screens display created, updated, and published timestamps with view counts.
