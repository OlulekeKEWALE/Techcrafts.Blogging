using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class Sponsorship : Entity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public string SponsorName { get; set; } = string.Empty;
    public string? SponsorLogoUrl { get; set; }
    public string? SponsorLink { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
