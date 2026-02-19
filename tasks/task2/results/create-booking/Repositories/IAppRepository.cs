using System;
using System.Linq.Expressions;

namespace create_booking.Repositories;

public interface IAppRepository<TEntity> where TEntity : class, new()
{
    Task<TEntity?> GetByIdAsync(int id);
    Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null);
    Task AddAsync(TEntity entity);
    Task SaveChangesAsync();
}
