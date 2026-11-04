using MediatR;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public class PublishPostCommandHandler : IRequestHandler<PublishPostCommand, bool>
{
    private readonly IBloggingDbContext _context;

    public PublishPostCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(PublishPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);
        if (post == null) return false;

        post.Status = PostStatus.Published;
        // Preserve original publication date if already set (idempotent publish)
        post.PublishedDate ??= DateTimeOffset.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return true;
    }
}
