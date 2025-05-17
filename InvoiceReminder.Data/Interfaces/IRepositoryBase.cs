using System.Linq.Expressions;

namespace InvoiceReminder.Data.Interfaces;

public interface IRepositoryBase<TEntity> : IDisposable where TEntity : class
{
    Task<TEntity> AddAsync(TEntity entity);
    Task<int> BulkInsertAsync(ICollection<TEntity> entities);
    void Remove(TEntity entity);
    Task<TEntity> GetByIdAsync(Guid id);
    IEnumerable<TEntity> GetAll();
    TEntity Update(TEntity entity);
    IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate);
}
