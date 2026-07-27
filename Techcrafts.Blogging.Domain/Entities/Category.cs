using Techcrafts.Blogging.SharedKernel.Entities;

namespace Techcrafts.Blogging.Domain.Entities;

public class Category : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
}
