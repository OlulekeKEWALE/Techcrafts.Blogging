using MediatR;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Commands;

public record CreatePostCommand(
    string Title,
    string Summary,
    string Content,
    string? FeaturedImageUrl,
    Guid AuthorId,
    List<Guid> TagIds,
    Guid CategoryId) : IRequest<Guid>;
