namespace InvoiceReminder.Domain.Extensions;

public static class EntityExtensions
{
    public static void AddIfNotExists<T>(this T entity, ICollection<T> collection) where T : class
    {
        if (entity is null)
        {
            return;
        }

        if (collection.FirstOrDefault(e => GetId(e).Equals(GetId(entity))) is null)
        {
            collection.Add(entity);
        }
    }

    private static Guid GetId<T>(T entity) where T : class
    {
        return (Guid)typeof(T).GetProperty("Id")!.GetValue(entity)!;
    }
}
