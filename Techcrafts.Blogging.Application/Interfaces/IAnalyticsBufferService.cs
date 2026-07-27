using System;

namespace Techcrafts.Blogging.Application.Interfaces;

public interface IAnalyticsBufferService
{
    void EnqueuePageView(Guid? postId, string? ipAddress, string? userAgent, string url);
}
