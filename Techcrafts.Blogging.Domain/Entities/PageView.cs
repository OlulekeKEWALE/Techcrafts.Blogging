using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class PageView : Entity
{
    public Guid? PostId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Referrer { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceType { get; set; }
    public string? CountryCode { get; set; }
    public string? SessionId { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}
