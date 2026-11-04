using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public class UnpublishPostCommandHandler : IRequestHandler<UnpublishPostCommand, bool>
{
    private readonly IBloggingDbContext _context;

    public UnpublishPostCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(UnpublishPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == request.PostId, cancellationToken);
        if (post == null) return false;

        post.Status = PostStatus.Draft;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
