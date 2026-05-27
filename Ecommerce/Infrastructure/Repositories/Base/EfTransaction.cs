using Application.Interfaces.Repositories.Base;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Base
{
    public class EfTransaction : ITransaction
    {
        private readonly IDbContextTransaction _dbContextTransaction;
        private readonly Func<CancellationToken, Task>? _onCommit;
        private readonly Func<CancellationToken, Task>? _onRollback;

        public EfTransaction(
            IDbContextTransaction dbContextTransaction,
            Func<CancellationToken, Task>? onCommit = null,
            Func<CancellationToken, Task>? onRollback = null)
        {
            _dbContextTransaction = dbContextTransaction ?? throw new ArgumentNullException(nameof(dbContextTransaction));
            _onCommit = onCommit;
            _onRollback = onRollback;
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
            if (_onCommit != null)
            {
                await _onCommit(cancellationToken);
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.RollbackAsync(cancellationToken);
            if (_onRollback != null)
            {
                await _onRollback(cancellationToken);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContextTransaction.DisposeAsync();
        }
    }
}
