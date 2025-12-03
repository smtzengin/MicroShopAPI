

using MicroShop.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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

    public async Task<(IEnumerable<T> Data, int TotalCount)> GetAllPagedAsync(
        int pageNumber,
        int pageSize,
        Expression<Func<T, bool>>? filter = null)
    {
        // Sorguyu oluştur 
        IQueryable<T> query = _dbSet;

        //  Filtre varsa uygula
        if (filter != null)
        {
            query = query.Where(filter);
        }

        //  Toplam kayıt sayısını al (Sayfalama hesabı için)
        int totalCount = await query.CountAsync();

        //  Sayfalama yap (Skip/Take) ve Veriyi Çek
        var data = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (data, totalCount);
    }
}