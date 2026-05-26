using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories.Base
{
    public interface ITransaction : IAsyncDisposable
    {
        Task CommitAsync(CancellationToken cancellationToken = default);
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}
