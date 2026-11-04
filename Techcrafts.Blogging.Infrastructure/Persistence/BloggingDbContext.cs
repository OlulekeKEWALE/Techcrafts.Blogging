using Microsoft.EntityFrameworkCore;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.SharedKernel.Entities;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace Techcrafts.Blogging.Infrastructure.Persistence;

public class BloggingDbContext : IdentityDbContext<IdentityUser>, IBloggingDbContext
{
    public BloggingDbContext(DbContextOptions<BloggingDbContext> options) : base(options)
    {
    }

    public DbSet<Post> Posts => Set<Post>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PostTag> PostTags => Set<PostTag>();
    public DbSet<Author> Authors => Set<Author>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<PostClap> PostClaps => Set<PostClap>();
    public DbSet<Bookmark> Bookmarks => Set<Bookmark>();
    public DbSet<NewsletterSubscription> NewsletterSubscriptions => Set<NewsletterSubscription>();
    public DbSet<PostEmbedding> PostEmbeddings => Set<PostEmbedding>();
    public DbSet<AiGenerationLog> AiGenerationLogs => Set<AiGenerationLog>();
    public DbSet<PageView> PageViews => Set<PageView>();
    public DbSet<PostReaction> PostReactions => Set<PostReaction>();
    public DbSet<Advertisement> Advertisements => Set<Advertisement>();
    public DbSet<Sponsorship> Sponsorships => Set<Sponsorship>();
    public DbSet<SiteSetting> SiteSettings => Set<SiteSetting>();
    public DbSet<TelemetryLog> TelemetryLogs => Set<TelemetryLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        builder.Entity<PostReaction>()
            .HasIndex(p => new { p.PostId, p.ReaderHash })
            .IsUnique();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = DateTimeOffset.UtcNow;
                    break;
            }
        }
        return base.SaveChangesAsync(cancellationToken);
    }
}
