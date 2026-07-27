using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;

namespace Techcrafts.Blogging.Application.Features.Engagement.Commands;

public class ClapPostCommandHandler : IRequestHandler<ClapPostCommand, int>
{
    private readonly IBloggingDbContext _context;

    public ClapPostCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(ClapPostCommand request, CancellationToken cancellationToken)
    {
        var clap = await _context.PostClaps
            .FirstOrDefaultAsync(c => c.PostId == request.PostId && c.ReaderIdentifier == request.ReaderIdentifier, cancellationToken);

        if (clap == null)
        {
            clap = new PostClap
            {
                PostId = request.PostId,
                ReaderIdentifier = request.ReaderIdentifier,
                Count = 1
            };
            _context.PostClaps.Add(clap);
        }
        else
        {
            if (clap.Count < 50)
            {
                clap.Count++;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Return total claps for the post
        var totalClaps = await _context.PostClaps
            .Where(c => c.PostId == request.PostId)
            .SumAsync(c => c.Count, cancellationToken);

        return totalClaps;
    }
}
