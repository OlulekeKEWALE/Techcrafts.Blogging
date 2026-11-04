using MediatR;
using Techcrafts.Blogging.Application.Common.Utils;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public class CreatePostCommandHandler : IRequestHandler<CreatePostCommand, Guid>
{
    private readonly IBloggingDbContext _context;

    public CreatePostCommandHandler(IBloggingDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreatePostCommand request, CancellationToken cancellationToken)
    {
        var slug = await SlugHelper.GenerateUniqueSlugAsync(_context, request.Title, null, cancellationToken);

        var post = new Post
        {
            Title = request.Title,
            Slug = slug,
            Summary = request.Summary,
            Content = request.Content,
            FeaturedImageUrl = request.FeaturedImageUrl,
            AuthorId = request.AuthorId,
            ReadingTimeMinutes = SlugHelper.CalculateReadingTime(request.Content),
            Status = request.Publish ? PostStatus.Published : PostStatus.Draft,
            PublishedDate = request.Publish ? DateTimeOffset.UtcNow : null
        };

        // Attach tags if provided
        if (request.TagIds != null && request.TagIds.Any())
        {
            foreach (var tagId in request.TagIds.Distinct())
            {
                post.PostTags.Add(new PostTag { TagId = tagId });
            }
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post.Id;
    }
}
