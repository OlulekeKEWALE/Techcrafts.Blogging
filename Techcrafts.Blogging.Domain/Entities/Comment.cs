using Techcrafts.Blogging.SharedKernel.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Domain.Entities;

public class Comment : Entity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public Guid? ParentCommentId { get; set; }
    public Comment? ParentComment { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string? UserId { get; set; }
    public string? AuthorWebsite { get; set; }
    public string Content { get; set; } = string.Empty;
    public CommentStatus Status { get; set; } = CommentStatus.Pending;
    public DateTimeOffset CreatedDate { get; set; }
    public string? IpAddress { get; set; }

    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
