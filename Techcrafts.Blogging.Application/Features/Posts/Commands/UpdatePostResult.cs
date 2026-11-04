namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public enum UpdatePostResultStatus
{
    Success,
    NotFound,
    ConcurrencyConflict
}

public record UpdatePostResult(
    UpdatePostResultStatus Status,
    string? CurrentTitle = null,
    string? CurrentSummary = null,
    string? CurrentContent = null,
    string? CurrentFeaturedImageUrl = null,
    bool? CurrentIsFeatured = null,
    List<string>? CurrentTagNames = null,
    string? NewRowVersion = null);
