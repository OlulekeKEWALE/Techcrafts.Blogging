using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class TelemetryLog : Entity
{
    public string TraceId { get; set; } = string.Empty;
    public string SpanName { get; set; } = string.Empty;
    public double DurationMs { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string? HttpMethod { get; set; }
    public string? HttpPath { get; set; }
    public int? HttpStatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}
