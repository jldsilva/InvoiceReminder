namespace InvoiceReminder.Domain.Extensions;

public static class EntityExtensions
{
    public static void AddIfNotExists<T>(this T entity, ICollection<T> collection) where T : class
    {
        if (entity is null)
        {
            return;
        }

        if (collection is HashSet<T> hashSet)
        {
            if (!hashSet.Any(e => EntityIdComparer<T>.GetId(e).Equals(EntityIdComparer<T>.GetId(entity))))
            {
                _ = hashSet.Add(entity);
            }

            return;
        }

        if (!collection.Any(e => EntityIdComparer<T>.GetId(e).Equals(EntityIdComparer<T>.GetId(entity))))
        {
            collection.Add(entity);
        }
    }
}

internal sealed class EntityIdComparer<T> : IEqualityComparer<T> where T : class
{
    public bool Equals(T x, T y)
    {
        return !(x == null || y == null) && GetId(x).Equals(GetId(y));
    }

    public int GetHashCode(T obj)
    {
        return obj == null ? 0 : GetId(obj).GetHashCode();
    }

    public static Guid GetId(T entity)
    {
        var property = typeof(T).GetProperty("Id")
            ?? throw new InvalidOperationException($"Type {typeof(T).Name} does not have an 'Id' property.");

        var value = property.GetValue(entity)
            ?? throw new InvalidOperationException($"The 'Id' property of {typeof(T).Name} is null.");

        return value is not Guid guidValue
            ? throw new InvalidOperationException($"The 'Id' property of {typeof(T).Name} is not of type Guid.")
            : guidValue;
    }
}
