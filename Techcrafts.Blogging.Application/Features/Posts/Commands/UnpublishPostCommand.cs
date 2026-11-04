using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public record UnpublishPostCommand(Guid PostId) : IRequest<bool>;
