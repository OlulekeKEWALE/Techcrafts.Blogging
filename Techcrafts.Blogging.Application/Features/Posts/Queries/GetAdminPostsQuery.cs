using MediatR;
using Techcrafts.Blogging.Domain.Enums;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record GetAdminPostsQuery(
    string? Search = null,
    PostStatus? Status = null,
    string? SortBy = "created_desc",
    int Page = 1,
    int PageSize = 10) : IRequest<PaginatedAdminPostsDto>;
