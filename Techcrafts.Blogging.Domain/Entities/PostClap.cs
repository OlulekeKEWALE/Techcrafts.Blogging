using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class PostClap : Entity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public string ReaderIdentifier { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
