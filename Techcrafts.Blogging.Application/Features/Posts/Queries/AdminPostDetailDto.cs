using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record AdminPostDetailDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? FeaturedImageUrl { get; init; }
    public PostStatus Status { get; init; }
    public Guid AuthorId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public DateTimeOffset CreatedDate { get; init; }
    public DateTimeOffset? UpdatedDate { get; init; }
    public DateTimeOffset? PublishedDate { get; init; }
    public int ViewCount { get; init; }
    public int ReadingTimeMinutes { get; init; }
    public bool IsFeatured { get; init; }
    public List<Guid> TagIds { get; init; } = new();
    public List<string> TagNames { get; init; } = new();
    public string? RowVersion { get; init; }
}
