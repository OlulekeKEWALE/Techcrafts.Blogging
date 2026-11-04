using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Common.Utils;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public class UpdatePostCommandHandler : IRequestHandler<UpdatePostCommand, UpdatePostResult>
{
    private readonly IBloggingDbContext _context;

    public UpdatePostCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<UpdatePostResult> Handle(UpdatePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (post == null)
            return new UpdatePostResult(UpdatePostResultStatus.NotFound);

        // Optimistic Concurrency check using RowVersion
        if (!string.IsNullOrEmpty(request.RowVersion))
        {
            try
            {
                var clientRowVersion = Convert.FromBase64String(request.RowVersion);
                if (post.RowVersion != null && !clientRowVersion.SequenceEqual(post.RowVersion))
                {
                    // Stale version detected immediately before DB round-trip
                    var currentTags = await _context.PostTags
                        .Where(pt => pt.PostId == post.Id)
                        .Select(pt => pt.Tag!.Name)
                        .ToListAsync(cancellationToken);

                    return new UpdatePostResult(
                        UpdatePostResultStatus.ConcurrencyConflict,
                        CurrentTitle: post.Title,
                        CurrentSummary: post.Summary,
                        CurrentContent: post.Content,
                        CurrentFeaturedImageUrl: post.FeaturedImageUrl,
                        CurrentIsFeatured: post.IsFeatured,
                        CurrentTagNames: currentTags,
                        NewRowVersion: Convert.ToBase64String(post.RowVersion));
                }

                // Attach original rowversion to EF Core entry for database-level atomic predicate check
                if (_context is DbContext efContext)
                {
                    efContext.Entry(post).Property(p => p.RowVersion).OriginalValue = clientRowVersion;
                }
            }
            catch (FormatException)
            {
                // Invalid Base64 rowversion format
            }
        }

        // Update core content
        post.Title = request.Title;
        post.Summary = request.Summary;
        post.Content = request.Content;
        post.FeaturedImageUrl = request.FeaturedImageUrl;
        post.IsFeatured = request.IsFeatured;
        post.ReadingTimeMinutes = SlugHelper.CalculateReadingTime(request.Content);

        // Lifecycle transitions based on user intent
        var action = request.Action?.Trim().ToLowerInvariant();
        switch (action)
        {
            case "publish":
                post.Status = PostStatus.Published;
                // Preserve first published date if already present
                post.PublishedDate ??= DateTimeOffset.UtcNow;
                break;

            case "unpublish":
            case "draft":
                post.Status = PostStatus.Draft;
                break;

            case "save":
            default:
                // Preserve existing Status
                break;
        }

        // Synchronize Tags
        if (request.TagIds != null)
        {
            var currentTagIds = post.PostTags.Select(pt => pt.TagId).ToList();
            var incomingTagIds = request.TagIds.Distinct().ToList();

            var toRemove = post.PostTags.Where(pt => !incomingTagIds.Contains(pt.TagId)).ToList();
            foreach (var removeTag in toRemove)
            {
                post.PostTags.Remove(removeTag);
            }

            var toAdd = incomingTagIds.Where(id => !currentTagIds.Contains(id)).ToList();
            foreach (var addTagId in toAdd)
            {
                post.PostTags.Add(new PostTag { PostId = post.Id, TagId = addTagId });
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return new UpdatePostResult(
                UpdatePostResultStatus.Success,
                NewRowVersion: post.RowVersion != null ? Convert.ToBase64String(post.RowVersion) : null);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Database-level concurrency conflict occurred (e.g. intervening update on SQL Server)
            var currentPost = await _context.Posts
                .AsNoTracking()
                .Include(p => p.PostTags).ThenInclude(pt => pt.Tag)
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (currentPost == null)
            {
                return new UpdatePostResult(UpdatePostResultStatus.NotFound);
            }

            return new UpdatePostResult(
                UpdatePostResultStatus.ConcurrencyConflict,
                CurrentTitle: currentPost.Title,
                CurrentSummary: currentPost.Summary,
                CurrentContent: currentPost.Content,
                CurrentFeaturedImageUrl: currentPost.FeaturedImageUrl,
                CurrentIsFeatured: currentPost.IsFeatured,
                CurrentTagNames: currentPost.PostTags.Select(pt => pt.Tag?.Name ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList(),
                NewRowVersion: currentPost.RowVersion != null ? Convert.ToBase64String(currentPost.RowVersion) : null);
        }
    }
}
