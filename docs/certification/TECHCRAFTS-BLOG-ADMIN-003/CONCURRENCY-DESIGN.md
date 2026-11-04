# TECHCRAFTS-BLOG-ADMIN-003 — Optimistic Concurrency Architecture & Design

## 1. Concurrency Control Strategy

To prevent lost updates when concurrent administrators edit blog posts, an explicit optimistic concurrency control mechanism was engineered using Microsoft SQL Server's native `rowversion` data type coupled with EF Core's change tracker.

### 1.1 Concurrency Token in Domain Entity
In `Techcrafts.Blogging.Domain/Entities/Post.cs`, the entity was augmented with a byte array concurrency property:
```csharp
public class Post : Entity
{
    // ... Other properties ...
    public byte[]? RowVersion { get; set; }
}
```

### 1.2 Entity Type Configuration
In `Techcrafts.Blogging.Infrastructure/Configurations/PostConfiguration.cs`:
```csharp
builder.Property(p => p.RowVersion)
    .IsRowVersion();
```
When configured with `.IsRowVersion()`, EF Core maps this property to the SQL Server `rowversion` (`timestamp`) column type, automatically configuring it as a concurrency token (`ValueGeneratedOnAddOrUpdate` and `ConcurrencyToken`).

---

## 2. CQRS Application Boundary Flow

### 2.1 Update Command & DTO
The `UpdatePostCommand` carries the client's original version token as a Base64-encoded string:
```csharp
public record UpdatePostCommand(
    Guid Id,
    string Title,
    string Summary,
    string Content,
    string? FeaturedImageUrl,
    bool IsFeatured,
    List<Guid>? TagIds,
    string Action,
    string? RowVersion = null
) : IRequest<UpdatePostResult>;
```

### 2.2 Application Handler Concurrency Interception
In `Techcrafts.Blogging.Application/Features/Posts/Commands/UpdatePostCommandHandler.cs`:
1. The post is loaded from the database with tracked change tracking.
2. If `request.RowVersion` is supplied, EF Core's original value buffer is assigned the client's token:
   ```csharp
   if (!string.IsNullOrEmpty(request.RowVersion))
   {
       var originalBytes = Convert.FromBase64String(request.RowVersion);
       _context.Entry(post).Property(p => p.RowVersion).OriginalValue = originalBytes;
   }
   ```
3. During `await _context.SaveChangesAsync(cancellationToken)`, EF Core generates an atomic SQL UPDATE predicate:
   ```sql
   UPDATE [Posts]
   SET [Title] = @p0, [Summary] = @p1, [Content] = @p2, ...
   WHERE [Id] = @p3 AND [RowVersion] = @p4;
   ```
4. If another transaction has updated the row in the interim, the `RowVersion` will have incremented. SQL Server reports 0 rows affected, causing EF Core to immediately throw `DbUpdateConcurrencyException`.
5. The handler catches `DbUpdateConcurrencyException`, queries the latest committed state from the database as `AsNoTracking()`, and returns an explicit domain result:
   ```csharp
   catch (DbUpdateConcurrencyException)
   {
       var currentPost = await _context.Posts
           .AsNoTracking()
           .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
           .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

       return UpdatePostResult.ConcurrencyConflict(
           currentTitle: currentPost?.Title,
           currentSummary: currentPost?.Summary,
           currentContent: currentPost?.Content,
           currentFeaturedImageUrl: currentPost?.FeaturedImageUrl,
           currentIsFeatured: currentPost?.IsFeatured,
           currentTagNames: currentPost?.PostTags.Select(pt => pt.Tag!.Name).ToList(),
           newRowVersion: currentPost?.RowVersion != null ? Convert.ToBase64String(currentPost.RowVersion) : null
       );
   }
   ```

---

## 3. Web Layer Presentation & Resolution Flow

### 3.1 Controller Handling (HTTP 409 Conflict)
In `Techcrafts.Blogging.Web/Controllers/AdminController.cs`:
```csharp
if (result.Status == UpdatePostResultStatus.ConcurrencyConflict)
{
    Response.StatusCode = StatusCodes.Status409Conflict;
    var conflictModel = new ConcurrencyConflictViewModel
    {
        PostId = id,
        SubmittedTitle = title,
        SubmittedSummary = summary,
        SubmittedContent = content,
        SubmittedFeaturedImageUrl = featuredImageUrl,
        SubmittedIsFeatured = isFeatured,
        SubmittedTagIds = tagIds,
        CurrentTitle = result.CurrentTitle ?? string.Empty,
        CurrentSummary = result.CurrentSummary ?? string.Empty,
        CurrentContent = result.CurrentContent ?? string.Empty,
        CurrentFeaturedImageUrl = result.CurrentFeaturedImageUrl,
        CurrentIsFeatured = result.CurrentIsFeatured ?? false,
        CurrentTagNames = result.CurrentTagNames ?? new List<string>(),
        NewRowVersion = result.NewRowVersion ?? string.Empty
    };
    return View("Conflict", conflictModel);
}
```

### 3.2 Side-by-Side Diff & Resolution UI (`Conflict.cshtml`)
The view `Views/Admin/Conflict.cshtml` renders:
- An alert banner indicating: `"Conflict: This post was modified by another user while you were editing it."`
- **Left Column ("Your Unsaved Version"):** Displays the administrator's exact submitted inputs (Title, Summary, Markdown Content, Featured Image, Status).
- **Right Column ("Current Persisted Version"):** Displays the newly committed database values.
- **Resolution Options:**
  1. **"Overwrite With My Version":** Submits the administrator's original form values accompanied by `NewRowVersion` (the refreshed concurrency token). Includes a JavaScript confirmation prompt before submission.
  2. **"Reload Latest Version":** Navigates back to `GET /admin/posts/{id}/edit`, discarding stale edits and populating the editor with the latest persisted database state.
