

using MicroShop.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MicroShop.Shared.Data;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbContext _context;
    private readonly DbSet<T> _dbSet;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = _context.Set<T>();
    }

    public async Task AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
    }
    public async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();

    public async Task<T> GetByIdAsync(object id) => await _dbSet.FindAsync(id);

    public void Remove(T entity)
    {
        _dbSet.Remove(entity);
    }
}