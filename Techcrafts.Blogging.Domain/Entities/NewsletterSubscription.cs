using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class NewsletterSubscription : Entity
{
    public string Email { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public string? VerificationToken { get; set; }
    public DateTimeOffset SubscribedDate { get; set; }
}
