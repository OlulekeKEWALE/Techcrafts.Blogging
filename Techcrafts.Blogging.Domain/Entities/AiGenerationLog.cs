using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class AiGenerationLog : Entity
{
    public string PromptType { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public string ModelUsed { get; set; } = string.Empty;
    public Guid CreatedBy { get; set; }
    public DateTimeOffset CreatedDate { get; set; }
}
