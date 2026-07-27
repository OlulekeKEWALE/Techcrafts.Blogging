using MediatR;

namespace Techcrafts.Blogging.Application.Features.Engagement.Commands;

public record ClapPostCommand(Guid PostId, string ReaderIdentifier) : IRequest<int>; // Returns new clap count
