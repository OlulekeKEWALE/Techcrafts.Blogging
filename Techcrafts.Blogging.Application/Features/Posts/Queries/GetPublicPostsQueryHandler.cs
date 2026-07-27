using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public class GetPublicPostsQueryHandler : IRequestHandler<GetPublicPostsQuery, PaginatedPostListDto>
{
    private readonly IBloggingDbContext _context;

    public GetPublicPostsQueryHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedPostListDto> Handle(GetPublicPostsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Posts
            .Where(p => p.Status == PostStatus.Published)
            .OrderByDescending(p => p.PublishedDate)
            .AsNoTracking();

        var totalCount = await query.CountAsync(cancellationToken);
        var posts = await query
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PostDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Summary = p.Summary,
                FeaturedImageUrl = p.FeaturedImageUrl,
                PublishedDate = p.PublishedDate,
                ReadingTimeMinutes = p.ReadingTimeMinutes,
                Tags = p.PostTags.Select(pt => new TagDto 
                { 
                    Id = pt.Tag!.Id, 
                    Name = pt.Tag.Name, 
                    Slug = pt.Tag.Slug 
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        var allTags = await _context.Tags
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                PostCount = t.PostTags.Count
            })
            .OrderByDescending(t => t.PostCount)
            .ToListAsync(cancellationToken);

        return new PaginatedPostListDto(totalCount, posts)
        {
            TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize),
            CurrentPage = request.Page,
            AllTags = allTags
        };
    }
}
