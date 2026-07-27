using Techcrafts.Blogging.SharedKernel.Entities;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Domain.Entities;

public class PostReaction : Entity
{
    public Guid PostId { get; set; }
    
    // Nullable FK to IdentityUser
    public string? UserId { get; set; }
    
    // SHA256 hash of IP + User-Agent for guest tracking
    public string ReaderHash { get; set; } = string.Empty;
    
    public ReactionType Reaction { get; set; }
    
    public DateTimeOffset CreatedDate { get; set; } = DateTimeOffset.UtcNow;
}
