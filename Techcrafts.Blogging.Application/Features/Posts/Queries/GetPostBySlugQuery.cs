using MediatR;

namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record GetPostBySlugQuery(string Slug, string? ReaderHash = null, bool AllowDraft = false) : IRequest<PostDto?>;
