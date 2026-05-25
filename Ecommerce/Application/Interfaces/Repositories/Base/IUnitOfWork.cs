using Domain.Common;
using Microsoft.EntityFrameworkCore.Storage;

namespace Application.Interfaces.Repositories.Base
{
    public interface IUnitOfWork
    {
        IGenericRepository<T> GetRepository<T>() where T : BaseEntity;
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
        bool HasActiveTransaction { get; }
        void ClearTracker();

    }
}
