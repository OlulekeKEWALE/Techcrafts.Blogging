using MediatR;
using Techcrafts.Blogging.Application.Interfaces;

namespace Techcrafts.Blogging.Application.Features.Analytics.Commands;

public class TrackPageViewCommandHandler : IRequestHandler<TrackPageViewCommand, bool>
{
    private readonly IAnalyticsBufferService _bufferService;

    public TrackPageViewCommandHandler(IAnalyticsBufferService bufferService)
    {
        _bufferService = bufferService;
    }

    public Task<bool> Handle(TrackPageViewCommand request, CancellationToken cancellationToken)
    {
        _bufferService.EnqueuePageView(request.PostId, request.IpAddress, request.UserAgent, request.Url);
        return Task.FromResult(true);
    }
}
