using MediatR;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Engagement.Commands;

public class ToggleReactionCommand : IRequest<ToggleReactionResult>
{
    public Guid PostId { get; set; }
    public string ReaderHash { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public ReactionType Reaction { get; set; }
}

public class ToggleReactionResult
{
    public bool Success { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    public int UserReaction { get; set; }
}
