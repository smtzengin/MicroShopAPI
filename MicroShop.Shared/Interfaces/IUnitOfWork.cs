
namespace MicroShop.Shared.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> Repository<T>() where T : class;
    Task<int> SaveChangesAsync();

    // Transaction Yönetimi
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
}
