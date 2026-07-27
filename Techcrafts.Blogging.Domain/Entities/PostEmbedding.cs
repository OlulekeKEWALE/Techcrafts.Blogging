using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class PostEmbedding : Entity
{
    public Guid PostId { get; set; }
    public Post? Post { get; set; }
    public int ChunkIndex { get; set; }
    public string ChunkContent { get; set; } = string.Empty;
    // We'll use a string or byte array to store vectors natively for MsSQL later, or just a comma-separated string for now until we configure the Vector extension
    public string Embedding { get; set; } = string.Empty; 
    public DateTimeOffset CreatedDate { get; set; }
}
