namespace Techcrafts.Blogging.SharedKernel.Entities;

public interface IAuditableEntity
{
    DateTimeOffset CreatedDate { get; set; }
    DateTimeOffset? UpdatedDate { get; set; }
    Guid? CreatedBy { get; set; }
    Guid? UpdatedBy { get; set; }
}
