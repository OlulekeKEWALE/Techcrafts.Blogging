using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;

namespace Techcrafts.Blogging.Application.Features.Engagement.Commands;

public class ToggleBookmarkCommandHandler : IRequestHandler<ToggleBookmarkCommand, bool>
{
    private readonly IBloggingDbContext _context;

    public ToggleBookmarkCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ToggleBookmarkCommand request, CancellationToken cancellationToken)
    {
        var existingBookmark = await _context.Bookmarks
            .FirstOrDefaultAsync(b => b.PostId == request.PostId && b.UserId == request.UserId, cancellationToken);

        if (existingBookmark != null)
        {
            _context.Bookmarks.Remove(existingBookmark);
            await _context.SaveChangesAsync(cancellationToken);
            return false; // Not bookmarked anymore
        }

        var newBookmark = new Bookmark
        {
            PostId = request.PostId,
            UserId = request.UserId
        };
        
        _context.Bookmarks.Add(newBookmark);
        await _context.SaveChangesAsync(cancellationToken);
        return true; // Bookmarked
    }
}
