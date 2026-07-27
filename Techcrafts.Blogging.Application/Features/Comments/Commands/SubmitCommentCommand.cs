using MediatR;

namespace Techcrafts.Blogging.Application.Features.Comments.Commands;

public record SubmitCommentCommand(
    Guid PostId,
    Guid? ParentCommentId,
    string AuthorName,
    string AuthorEmail,
    string? AuthorWebsite,
    string Content,
    string IpAddress,
    string? UserId = null,
    string? AuthorAvatarUrl = null) : IRequest<Guid>;
