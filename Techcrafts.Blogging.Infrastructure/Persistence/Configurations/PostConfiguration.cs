using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techcrafts.Blogging.Domain.Entities;

namespace Techcrafts.Blogging.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.HasIndex(p => new { p.Status, p.PublishedDate }).IsDescending(false, true);

        builder.Property(p => p.RowVersion)
               .IsRowVersion();

        builder.HasMany(p => p.Comments)
               .WithOne(c => c.Post)
               .HasForeignKey(c => c.PostId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
