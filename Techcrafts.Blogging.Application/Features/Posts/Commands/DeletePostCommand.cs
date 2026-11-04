using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public record DeletePostCommand(Guid PostId) : IRequest<bool>;
