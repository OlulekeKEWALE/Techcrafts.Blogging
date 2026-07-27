using System;
using MediatR;

namespace Techcrafts.Blogging.Application.Features.Analytics.Commands;

public record TrackPageViewCommand(Guid? PostId, string? IpAddress, string? UserAgent, string Url) : IRequest<bool>;
