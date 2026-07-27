using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Techcrafts.Blogging.Domain.Entities;

namespace Techcrafts.Blogging.Infrastructure.Persistence.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.HasKey(s => s.Key);
    }
}
