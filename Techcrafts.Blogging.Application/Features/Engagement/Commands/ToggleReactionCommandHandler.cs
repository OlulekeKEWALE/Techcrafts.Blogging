using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Engagement.Commands;

public class ToggleReactionCommandHandler : IRequestHandler<ToggleReactionCommand, ToggleReactionResult>
{
    private readonly IBloggingDbContext _context;

    public ToggleReactionCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<ToggleReactionResult> Handle(ToggleReactionCommand request, CancellationToken cancellationToken)
    {
        var existingReaction = await _context.PostReactions
            .FirstOrDefaultAsync(r => r.PostId == request.PostId && r.ReaderHash == request.ReaderHash, cancellationToken);

        if (existingReaction != null)
        {
            if (existingReaction.Reaction == request.Reaction)
            {
                // Toggle off
                _context.PostReactions.Remove(existingReaction);
                request.Reaction = ReactionType.None;
            }
            else
            {
                // Switch reaction
                existingReaction.Reaction = request.Reaction;
                existingReaction.UserId = request.UserId; // Update User ID if they logged in
            }
        }
        else
        {
            // New reaction
            var newReaction = new PostReaction
            {
                PostId = request.PostId,
                ReaderHash = request.ReaderHash,
                UserId = request.UserId,
                Reaction = request.Reaction,
                CreatedDate = DateTimeOffset.UtcNow
            };
            _context.PostReactions.Add(newReaction);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Fetch new counts
        var reactions = await _context.PostReactions
            .Where(r => r.PostId == request.PostId)
            .ToListAsync(cancellationToken);

        return new ToggleReactionResult
        {
            Success = true,
            LikesCount = reactions.Count(r => r.Reaction == ReactionType.Like),
            DislikesCount = reactions.Count(r => r.Reaction == ReactionType.Dislike),
            UserReaction = (int)request.Reaction
        };
    }
}
