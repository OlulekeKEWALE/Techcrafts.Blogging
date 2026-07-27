using MediatR;
using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Application.Features.Posts.Queries;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public class GetPostBySlugQueryHandler : IRequestHandler<GetPostBySlugQuery, PostDto?>
{
    private readonly IBloggingDbContext _context;

    public GetPostBySlugQueryHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<PostDto?> Handle(GetPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var post = await _context.Posts
            .Include(p => p.PostTags)
                .ThenInclude(pt => pt.Tag)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == request.Slug, cancellationToken);

        if (post == null)
            return null;

        var reactions = await _context.PostReactions
            .Where(r => r.PostId == post.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        int userReaction = 0;
        if (!string.IsNullOrEmpty(request.ReaderHash))
        {
            var r = reactions.FirstOrDefault(x => x.ReaderHash == request.ReaderHash);
            if (r != null) userReaction = (int)r.Reaction;
        }

        return new PostDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Summary = post.Summary,
            Content = post.Content,
            FeaturedImageUrl = post.FeaturedImageUrl,
            PublishedDate = post.PublishedDate,
            ReadingTimeMinutes = post.ReadingTimeMinutes,
            Tags = post.PostTags.Select(pt => new TagDto 
            { 
                Id = pt.Tag!.Id, 
                Name = pt.Tag.Name, 
                Slug = pt.Tag.Slug 
            }).ToList(),
            LikesCount = reactions.Count(x => x.Reaction == Techcrafts.Blogging.Domain.Enums.ReactionType.Like),
            DislikesCount = reactions.Count(x => x.Reaction == Techcrafts.Blogging.Domain.Enums.ReactionType.Dislike),
            UserReaction = userReaction,
            Comments = await GetCommentsTreeAsync(post.Id, cancellationToken)
        };
    }

    private async Task<List<Techcrafts.Blogging.Application.Features.Comments.Queries.CommentDto>> GetCommentsTreeAsync(Guid postId, CancellationToken cancellationToken)
    {
        var allComments = await _context.Comments
            .Where(c => c.PostId == postId && c.Status == Techcrafts.Blogging.Domain.Enums.CommentStatus.Approved)
            .OrderBy(c => c.CreatedDate)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var dtos = allComments.Select(c => new Techcrafts.Blogging.Application.Features.Comments.Queries.CommentDto
        {
            Id = c.Id,
            ParentCommentId = c.ParentCommentId,
            AuthorName = c.AuthorName,
            AuthorAvatarUrl = c.AuthorAvatarUrl,
            Content = c.Content,
            CreatedDate = c.CreatedDate,
            Status = c.Status.ToString()
        }).ToList();

        // Build the tree
        var rootComments = dtos.Where(c => c.ParentCommentId == null).ToList();
        foreach (var c in dtos)
        {
            if (c.ParentCommentId != null)
            {
                var parent = dtos.FirstOrDefault(p => p.Id == c.ParentCommentId);
                if (parent != null)
                {
                    parent.Replies.Add(c);
                }
            }
        }

        return rootComments;
    }
}
