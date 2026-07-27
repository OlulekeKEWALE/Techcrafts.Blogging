namespace Techcrafts.Blogging.Application.Features.Comments.Queries;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid? ParentCommentId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTimeOffset CreatedDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<CommentDto> Replies { get; set; } = new();
}
