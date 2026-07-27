using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Techcrafts.Blogging.Application.Interfaces;
using Techcrafts.Blogging.Domain.Entities;
using System.Collections.Concurrent;

namespace Techcrafts.Blogging.Infrastructure.Services;

public class AnalyticsBufferService : BackgroundService, IAnalyticsBufferService
{
    private readonly ConcurrentQueue<PageView> _pageViewQueue = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(10);

    public AnalyticsBufferService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public void EnqueuePageView(Guid? postId, string? ipAddress, string? userAgent, string url)
    {
        _pageViewQueue.Enqueue(new PageView
        {
            PostId = postId,
            Url = url,
            SessionId = ipAddress,
            UserAgent = userAgent,
            Timestamp = DateTimeOffset.UtcNow
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_flushInterval, stoppingToken);
            await FlushQueueAsync();
        }
    }

    private async Task FlushQueueAsync()
    {
        if (_pageViewQueue.IsEmpty) return;

        var viewsToInsert = new List<PageView>();
        while (_pageViewQueue.TryDequeue(out var view))
        {
            viewsToInsert.Add(view);
        }

        if (viewsToInsert.Any())
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IBloggingDbContext>();
            
            context.PageViews.AddRange(viewsToInsert);
            await context.SaveChangesAsync(CancellationToken.None);
        }
    }
}
