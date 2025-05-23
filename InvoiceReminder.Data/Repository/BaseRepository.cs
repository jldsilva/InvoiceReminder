using EFCore.BulkExtensions;
using InvoiceReminder.Data.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace InvoiceReminder.Data.Repository;

public class BaseRepository<TDbContext, TEntity> : IBaseRepository<TEntity>
    where TDbContext : DbContext where TEntity : class
{
    private readonly TDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;
    private bool disposed = false;

    public BaseRepository(TDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = _dbContext.Set<TEntity>();
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        _ = await _dbSet.AddAsync(entity);

        return entity;
    }

    public virtual async Task<int> BulkInsertAsync(ICollection<TEntity> entities)
    {
        foreach (var entity in entities)
        {
            entity.GetType().GetProperty("CreatedAt")?.SetValue(entity, DateTime.Now);
            entity.GetType().GetProperty("UpdatedAt")?.SetValue(entity, DateTime.Now);
        }

        await _dbContext.BulkInsertAsync(entities);

        return entities.Count;
    }

    public virtual void Remove(TEntity entity)
    {
        if (_dbContext.Entry(entity).State == EntityState.Detached)
        {
            _ = _dbSet.Attach(entity);
        }

        _ = _dbSet.Remove(entity);
    }

    public virtual async Task<TEntity> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual IEnumerable<TEntity> GetAll()
    {
        return _dbSet.AsNoTracking().AsEnumerable();
    }

    public virtual TEntity Update(TEntity entity)
    {
        if (_dbContext.Entry(entity).State == EntityState.Detached)
        {
            _ = _dbSet.Attach(entity);
        }

        _ = _dbSet.Update(entity);

        return entity;
    }

    public virtual IEnumerable<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
    {
        return _dbSet.Where(predicate).AsEnumerable();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed && disposing)
        {
            _dbContext.Dispose();
        }

        disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
