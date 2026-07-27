using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Domain.Entities;

namespace Techcrafts.Blogging.Application.Interfaces;

public interface IBloggingDbContext
{
    DbSet<Post> Posts { get; }
    DbSet<Category> Categories { get; }
    DbSet<Tag> Tags { get; }
    DbSet<PostTag> PostTags { get; }
    DbSet<Author> Authors { get; }
    DbSet<Comment> Comments { get; }
    DbSet<PostClap> PostClaps { get; }
    DbSet<Bookmark> Bookmarks { get; }
    DbSet<NewsletterSubscription> NewsletterSubscriptions { get; }
    DbSet<PostEmbedding> PostEmbeddings { get; }
    DbSet<AiGenerationLog> AiGenerationLogs { get; }
    DbSet<PageView> PageViews { get; }
    DbSet<PostReaction> PostReactions { get; }
    DbSet<Advertisement> Advertisements { get; }
    DbSet<Sponsorship> Sponsorships { get; }
    DbSet<SiteSetting> SiteSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
