using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record GetAdminPostByIdQuery(Guid Id) : IRequest<AdminPostDetailDto?>;
