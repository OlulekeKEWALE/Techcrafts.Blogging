using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record AdminPostListItemDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string? FeaturedImageUrl { get; init; }
    public PostStatus Status { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public Guid AuthorId { get; init; }
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset? UpdatedDate { get; init; }
    public DateTimeOffset? PublishedDate { get; init; }
    public int ViewCount { get; init; }
    public int ReadingTimeMinutes { get; init; }
    public bool IsFeatured { get; init; }
    public int CommentCount { get; init; }
    public List<string> Tags { get; init; } = new();
}

public record PaginatedAdminPostsDto(
    int TotalCount,
    List<AdminPostListItemDto> Posts,
    int Page,
    int PageSize,
    int TotalPages,
    int TotalDrafts,
    int TotalPublished,
    int TotalScheduled,
    int TotalArchived);
