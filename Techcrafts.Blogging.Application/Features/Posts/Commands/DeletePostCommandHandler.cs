using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public class DeletePostCommandHandler : IRequestHandler<DeletePostCommand, bool>
{
    private readonly IBloggingDbContext _context;

    public DeletePostCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeletePostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
            .Include(p => p.Comments)
            .FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);

        if (post == null)
            return false;

        // Clean up dependent reaction/view/clap/bookmark/embedding records before deletion
        var reactions = await _context.PostReactions.Where(r => r.PostId == post.Id).ToListAsync(cancellationToken);
        if (reactions.Any()) _context.PostReactions.RemoveRange(reactions);

        var claps = await _context.PostClaps.Where(c => c.PostId == post.Id).ToListAsync(cancellationToken);
        if (claps.Any()) _context.PostClaps.RemoveRange(claps);

        var bookmarks = await _context.Bookmarks.Where(b => b.PostId == post.Id).ToListAsync(cancellationToken);
        if (bookmarks.Any()) _context.Bookmarks.RemoveRange(bookmarks);

        var pageViews = await _context.PageViews.Where(pv => pv.PostId == post.Id).ToListAsync(cancellationToken);
        if (pageViews.Any()) _context.PageViews.RemoveRange(pageViews);

        var embeddings = await _context.PostEmbeddings.Where(e => e.PostId == post.Id).ToListAsync(cancellationToken);
        if (embeddings.Any()) _context.PostEmbeddings.RemoveRange(embeddings);

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
