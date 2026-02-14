using System;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;  

namespace create_booking.Repositories;

public class AppRepository<TEntity> : IAppRepository<TEntity> where TEntity : class, new()
{
    private readonly Data.AppDbContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public AppRepository(Data.AppDbContext context)
    {
        _context = context;
        _dbSet = _context.Set<TEntity>();
    }

    public async Task<TEntity?> GetByIdAsync(int id)
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

    public async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null)
    {
        return await _dbSet.Where(predicate ?? (x => true)).ToListAsync();
    }
}
