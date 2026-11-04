using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public record UpdatePostCommand(
    Guid Id,
    string Title,
    string Summary,
    string Content,
    string? FeaturedImageUrl,
    bool IsFeatured,
    List<Guid>? TagIds,
    string Action,
    string? RowVersion = null) : IRequest<UpdatePostResult>;
