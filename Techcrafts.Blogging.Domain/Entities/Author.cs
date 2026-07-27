using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class Author : Entity
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public string Email { get; set; } = string.Empty;
    public ICollection<Post> Posts { get; set; } = new List<Post>();
}
