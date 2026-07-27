using MediatR;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Comments.Commands;

public class SubmitCommentCommandHandler : IRequestHandler<SubmitCommentCommand, Guid>
{
    private readonly IBloggingDbContext _context;

    public SubmitCommentCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(SubmitCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new Comment
        {
            PostId = request.PostId,
            ParentCommentId = request.ParentCommentId,
            AuthorName = request.AuthorName,
            AuthorEmail = request.AuthorEmail,
            AuthorWebsite = request.AuthorWebsite,
            Content = request.Content,
            IpAddress = request.IpAddress,
            UserId = request.UserId,
            AuthorAvatarUrl = request.AuthorAvatarUrl,
            Status = string.IsNullOrEmpty(request.UserId) ? CommentStatus.Pending : CommentStatus.Approved
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        // Would publish CommentSubmittedNotification here

        return comment.Id;
    }
}
