using System;
using Microsoft.EntityFrameworkCore;

namespace booking_history_service.Repositories;

public class AppRepository<TEntity> : IAppRepository<TEntity> where TEntity : class, new()
{
    private readonly Data.AppDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public AppRepository(Data.AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(string id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
