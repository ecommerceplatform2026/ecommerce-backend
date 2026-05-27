using Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories.Base
{
    public interface IUnitOfWork
    {
        IGenericRepository<T> GetRepository<T>() where T : BaseEntity;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        bool HasActiveTransaction { get; }
        void ClearTracker();
        IExecutionStrategy CreateExecutionStrategy();
    }
}
