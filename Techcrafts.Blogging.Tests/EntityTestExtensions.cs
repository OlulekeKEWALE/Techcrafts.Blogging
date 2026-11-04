using System.Reflection;
using Techcrafts.Blogging.SharedKernel.Entities;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Techcrafts.Blogging.Tests;

public static class EntityTestExtensions
{
    public static T WithId<T>(this T entity, Guid id) where T : Entity
    {
        var idProp = typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Public | BindingFlags.Instance);
        idProp?.SetValue(entity, id);
        return entity;
    }
}
