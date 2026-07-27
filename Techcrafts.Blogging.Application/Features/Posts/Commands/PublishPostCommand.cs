using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public record PublishPostCommand(Guid PostId) : IRequest<bool>;
