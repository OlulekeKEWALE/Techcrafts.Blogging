namespace Techcrafts.Blogging.Domain.Entities;

public class SiteSetting
{
    public string Key { get; set; } = string.Empty; // PK
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid? UpdatedBy { get; set; }
    public DateTimeOffset LastUpdated { get; set; }
}
