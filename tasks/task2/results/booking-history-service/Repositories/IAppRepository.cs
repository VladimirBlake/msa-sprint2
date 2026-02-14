using System;

namespace booking_history_service.Repositories;

public interface IAppRepository<TEntity> where TEntity : class, new()
{
    Task<TEntity?> GetByIdAsync(string id);
    Task AddAsync(TEntity entity);
    Task SaveChangesAsync();
}
