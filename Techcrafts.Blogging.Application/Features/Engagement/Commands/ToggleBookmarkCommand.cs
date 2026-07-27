using MediatR;

namespace Techcrafts.Blogging.Application.Features.Engagement.Commands;

public record ToggleBookmarkCommand(Guid PostId, Guid UserId) : IRequest<bool>;
