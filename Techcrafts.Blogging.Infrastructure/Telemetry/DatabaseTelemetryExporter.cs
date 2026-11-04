using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using Techcrafts.Blogging.Domain.Entities;
using Techcrafts.Blogging.Infrastructure.Persistence;

namespace Techcrafts.Blogging.Infrastructure.Telemetry;

public class DatabaseTelemetryExporter : BaseExporter<Activity>
{
    private readonly IServiceProvider _serviceProvider;

    public DatabaseTelemetryExporter(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override ExportResult Export(in Batch<Activity> batch)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BloggingDbContext>();
            
            var logs = new List<TelemetryLog>();

            foreach (var activity in batch)
            {
                // Skip EF Core internal queries to prevent recursive loop!
                if (activity.DisplayName == "Execute DbCommand" || activity.DisplayName.Contains("EFCore") || activity.Source.Name.Contains("EntityFrameworkCore"))
                    continue;
                    
                // Also skip if it's the telemetry endpoint itself to keep it clean
                var path = activity.GetTagItem("http.target")?.ToString() ?? activity.GetTagItem("url.path")?.ToString();
                if (path != null && (path.StartsWith("/Telemetry") || path.StartsWith("/css") || path.StartsWith("/js") || path.StartsWith("/lib") || path.StartsWith("/images")))
                    continue;

                var log = new TelemetryLog
                {
                    TraceId = activity.TraceId.ToString(),
                    SpanName = activity.DisplayName,
                    DurationMs = activity.Duration.TotalMilliseconds,
                    Timestamp = activity.StartTimeUtc,
                    HttpMethod = activity.GetTagItem("http.method")?.ToString() ?? activity.GetTagItem("http.request.method")?.ToString(),
                    HttpPath = path,
                    HttpStatusCode = GetStatusCode(activity),
                    ErrorMessage = activity.GetTagItem("error.message")?.ToString() ?? activity.GetTagItem("exception.message")?.ToString()
                };

                // We only care about HTTP requests for this dashboard
                if (!string.IsNullOrEmpty(log.HttpPath) && activity.Kind == ActivityKind.Server)
                {
                    logs.Add(log);
                }
            }

            if (logs.Any())
            {
                try 
                {
                    // Suppress flow so the DB insert itself doesn't generate NEW telemetry traces
                    using (ExecutionContext.SuppressFlow())
                    {
                        dbContext.TelemetryLogs.AddRange(logs);
                        dbContext.SaveChanges();
                    }
                }
                catch 
                {
                    // If it fails, we don't want to crash the exporter thread
                    return ExportResult.Failure;
                }
            }

            return ExportResult.Success;
        }
        catch (ObjectDisposedException)
        {
            return ExportResult.Failure;
        }
        catch (Exception)
        {
            return ExportResult.Failure;
        }
    }
    
    private int? GetStatusCode(Activity activity)
    {
        var statusCodeStr = activity.GetTagItem("http.status_code")?.ToString() ?? activity.GetTagItem("http.response.status_code")?.ToString();
        if (int.TryParse(statusCodeStr, out var statusCode))
        {
            return statusCode;
        }
        return null;
    }
}
