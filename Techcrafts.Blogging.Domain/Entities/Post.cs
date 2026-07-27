using Techcrafts.Blogging.SharedKernel.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Domain.Entities;

public class Post : Entity, IAuditableEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public Guid AuthorId { get; set; }
    public PostStatus Status { get; set; } = PostStatus.Draft;
    public DateTimeOffset? PublishedDate { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
    public DateTimeOffset? UpdatedDate { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }
    public int ViewCount { get; set; }
    public int ReadingTimeMinutes { get; set; }
    public bool IsFeatured { get; set; }

    public Author? Author { get; set; }
    public ICollection<PostTag> PostTags { get; set; } = new List<PostTag>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
}
