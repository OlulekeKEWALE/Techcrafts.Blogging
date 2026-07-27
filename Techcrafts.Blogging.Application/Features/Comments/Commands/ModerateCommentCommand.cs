using MediatR;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Comments.Commands;

public record ModerateCommentCommand(Guid CommentId, CommentStatus Status) : IRequest<bool>;
