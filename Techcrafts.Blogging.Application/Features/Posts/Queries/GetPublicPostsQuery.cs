using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record GetPublicPostsQuery(int Page = 1, int PageSize = 10, string? CategorySlug = null, string? TagSlug = null) : IRequest<PaginatedPostListDto>;
