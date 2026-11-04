using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public class GetAdminPostByIdQueryHandler : IRequestHandler<GetAdminPostByIdQuery, AdminPostDetailDto?>
{
    private readonly IBloggingDbContext _context;

    public GetAdminPostByIdQueryHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<AdminPostDetailDto?> Handle(GetAdminPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .AsNoTracking()
            .Include(p => p.Author)
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (post == null)
            return null;

        return new AdminPostDetailDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Content = post.Content,
            FeaturedImageUrl = post.FeaturedImageUrl,
            Status = post.Status,
            AuthorId = post.AuthorId,
            AuthorName = post.Author?.DisplayName ?? "Unknown Author",
            CreatedDate = post.CreatedDate,
            UpdatedDate = post.UpdatedDate,
            PublishedDate = post.PublishedDate,
            ViewCount = post.ViewCount,
            ReadingTimeMinutes = post.ReadingTimeMinutes,
            IsFeatured = post.IsFeatured,
            TagIds = post.PostTags.Select(pt => pt.TagId).ToList(),
            TagNames = post.PostTags.Select(pt => pt.Tag?.Name ?? string.Empty).Where(n => !string.IsNullOrEmpty(n)).ToList(),
            RowVersion = post.RowVersion != null ? Convert.ToBase64String(post.RowVersion) : null
        };
    }
}
