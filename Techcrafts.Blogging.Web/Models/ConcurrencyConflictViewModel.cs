using Techcrafts.Blogging.Application.Features.Posts.Commands;

namespace Techcrafts.Blogging.Web.Models;

public class ConcurrencyConflictViewModel
{
    public Guid PostId { get; set; }
    public string SubmittedTitle { get; set; } = string.Empty;
    public string SubmittedSummary { get; set; } = string.Empty;
    public string SubmittedContent { get; set; } = string.Empty;
    public string? SubmittedFeaturedImageUrl { get; set; }
    public bool SubmittedIsFeatured { get; set; }
    public List<Guid>? SubmittedTagIds { get; set; }

    public string CurrentTitle { get; set; } = string.Empty;
    public string CurrentSummary { get; set; } = string.Empty;
    public string CurrentContent { get; set; } = string.Empty;
    public string? CurrentFeaturedImageUrl { get; set; }
    public bool CurrentIsFeatured { get; set; }
    public List<string> CurrentTagNames { get; set; } = new();

    public string NewRowVersion { get; set; } = string.Empty;
}
