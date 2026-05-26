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

        public EfTransaction(IDbContextTransaction dbContextTransaction)
        {
            _dbContextTransaction = dbContextTransaction ?? throw new ArgumentNullException(nameof(dbContextTransaction));
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.CommitAsync(cancellationToken);
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await _dbContextTransaction.RollbackAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            await _dbContextTransaction.DisposeAsync();
        }
    }
}
