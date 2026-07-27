namespace Techcrafts.Blogging.Application.Features.Posts.Queries;

public record PaginatedPostListDto(int TotalCount, IEnumerable<PostDto> Posts)
{
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public IEnumerable<TagDto> AllTags { get; set; } = new List<TagDto>();
}
