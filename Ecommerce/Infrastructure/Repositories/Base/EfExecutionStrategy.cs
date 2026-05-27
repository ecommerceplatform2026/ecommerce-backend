using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Repositories.Base;

namespace Infrastructure.Repositories.Base
{
    public class EfExecutionStrategy : IExecutionStrategy
    {
        private readonly Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy _strategy;

        public EfExecutionStrategy(Microsoft.EntityFrameworkCore.Storage.IExecutionStrategy strategy)
        {
            _strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
        }

        public async Task ExecuteAsync(Func<Task> operation)
        {
            await _strategy.ExecuteAsync(operation);
        }

        public async Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation)
        {
            return await _strategy.ExecuteAsync(operation);
        }
    }
}
