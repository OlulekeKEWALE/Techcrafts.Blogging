namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public class PostDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public DateTimeOffset? PublishedDate { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public IEnumerable<TagDto> Tags { get; set; } = new List<TagDto>();
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public int UserReaction { get; set; }
    public IEnumerable<Techcrafts.Blogging.Application.Features.Comments.Queries.CommentDto> Comments { get; set; } = new List<Techcrafts.Blogging.Application.Features.Comments.Queries.CommentDto>();
}
