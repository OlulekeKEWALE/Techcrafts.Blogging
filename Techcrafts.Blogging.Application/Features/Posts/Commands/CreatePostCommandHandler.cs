using MediatR;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using System.Text.RegularExpressions;

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
        var post = new Post
        {
            Title = request.Title,
            Slug = GenerateSlug(request.Title),
            Summary = request.Summary,
            Content = request.Content,
            FeaturedImageUrl = request.FeaturedImageUrl,
            AuthorId = request.AuthorId,
            ReadingTimeMinutes = CalculateReadingTime(request.Content)
        };

        // Attach category if provided
        // (In a real app, validate existence)
        
        // Attach tags
        if (request.TagIds != null && request.TagIds.Any())
        {
            foreach (var tagId in request.TagIds)
            {
                post.PostTags.Add(new PostTag { TagId = tagId });
            }
        }

        _context.Posts.Add(post);
        await _context.SaveChangesAsync(cancellationToken);

        return post.Id;
    }

    private string GenerateSlug(string phrase)
    {
        string str = phrase.ToLowerInvariant();
        // Remove invalid chars
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
        // Convert multiple spaces into one space
        str = Regex.Replace(str, @"\s+", " ").Trim();
        // Cut and trim
        str = str.Substring(0, str.Length <= 45 ? str.Length : 45).Trim();
        // Hyphens
        str = Regex.Replace(str, @"\s", "-");
        return str;
    }

    private int CalculateReadingTime(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 1;
        var wordCount = text.Split(new char[] { ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)Math.Ceiling(wordCount / 200.0);
    }
}
