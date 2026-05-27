using System;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories.Base
{
    public interface IExecutionStrategy
    {
        Task ExecuteAsync(Func<Task> operation);
        Task<TResult> ExecuteAsync<TResult>(Func<Task<TResult>> operation);
    }
}
