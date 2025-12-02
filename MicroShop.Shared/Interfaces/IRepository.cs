
namespace MicroShop.Shared.Interfaces;

public interface IRepository<T> where T : class
{
    Task AddAsync(T entity);
    Task<T> GetByIdAsync(object id);
    Task<IEnumerable<T>> GetAllAsync();
    void Remove(T entity);
}