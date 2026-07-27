using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class Advertisement : Entity
{
    public string Title { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string TargetUrl { get; set; } = string.Empty;
    public string PlacementLocation { get; set; } = string.Empty;
    public int ImpressionsCount { get; set; }
    public int ClickCount { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
}
