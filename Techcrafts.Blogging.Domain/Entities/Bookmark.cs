using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class Bookmark : Entity
{
    public Guid UserId { get; set; }
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
