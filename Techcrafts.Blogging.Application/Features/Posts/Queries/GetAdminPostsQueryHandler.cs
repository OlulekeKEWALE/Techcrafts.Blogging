using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public class GetAdminPostsQueryHandler : IRequestHandler<GetAdminPostsQuery, PaginatedAdminPostsDto>
{
    private readonly IBloggingDbContext _context;

    public GetAdminPostsQueryHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedAdminPostsDto> Handle(GetAdminPostsQuery request, CancellationToken cancellationToken)
    {
        // Enforce safe pagination boundaries
        const int maxPageSize = 50;
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 10 : (request.PageSize > maxPageSize ? maxPageSize : request.PageSize);

        // Status counts across all posts for admin tab counters
        var statusCounts = await _context.Posts
            .GroupBy(p => p.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int totalDrafts = statusCounts.FirstOrDefault(x => x.Status == PostStatus.Draft)?.Count ?? 0;
        int totalPublished = statusCounts.FirstOrDefault(x => x.Status == PostStatus.Published)?.Count ?? 0;
        int totalScheduled = statusCounts.FirstOrDefault(x => x.Status == PostStatus.Scheduled)?.Count ?? 0;
        int totalArchived = statusCounts.FirstOrDefault(x => (int)x.Status == 3)?.Count ?? 0;

        var query = _context.Posts.AsNoTracking().AsQueryable();

        // Status filter
        if (request.Status.HasValue)
        {
            query = query.Where(p => p.Status == request.Status.Value);
        }

        // Search filter (Title, Summary, Slug)
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(p =>
                p.Title.ToLower().Contains(search) ||
                p.Summary.ToLower().Contains(search) ||
                p.Slug.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Stable ordering with tie-breaker
        query = request.SortBy switch
        {
            "created_asc" => query.OrderBy(p => p.CreatedDate).ThenBy(p => p.Id),
            "title_asc" => query.OrderBy(p => p.Title).ThenBy(p => p.Id),
            "title_desc" => query.OrderByDescending(p => p.Title).ThenBy(p => p.Id),
            "views_desc" => query.OrderByDescending(p => p.ViewCount).ThenBy(p => p.Id),
            "published_desc" => query.OrderByDescending(p => p.PublishedDate).ThenBy(p => p.Id),
            _ => query.OrderByDescending(p => p.CreatedDate).ThenBy(p => p.Id)
        };

        var posts = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new AdminPostListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                FeaturedImageUrl = p.FeaturedImageUrl,
                Status = p.Status,
                AuthorName = p.Author != null ? p.Author.DisplayName : "Unknown Author",
                AuthorId = p.AuthorId,
                CreatedDate = p.CreatedDate,
                UpdatedDate = p.UpdatedDate,
                PublishedDate = p.PublishedDate,
                ViewCount = p.ViewCount,
                ReadingTimeMinutes = p.ReadingTimeMinutes,
                IsFeatured = p.IsFeatured,
                CommentCount = p.Comments.Count,
                Tags = p.PostTags.Select(pt => pt.Tag!.Name).ToList()
            })
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PaginatedAdminPostsDto(
            totalCount,
            posts,
            page,
            pageSize,
            totalPages,
            totalDrafts,
            totalPublished,
            totalScheduled,
            totalArchived);
    }
}
