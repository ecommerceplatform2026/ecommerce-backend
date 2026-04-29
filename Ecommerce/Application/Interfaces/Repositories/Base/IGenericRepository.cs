using Domain.Common;
using System.Linq.Expressions;

namespace Application.Interfaces.Repositories.Base
{
    public interface IGenericRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

        Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

        Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

        Task<(List<T> Items, int TotalCount)> GetPagedAsync(
            int page,
            int pageSize,
            Expression<Func<T, bool>>? filter = null,
            Expression<Func<T, object>>? orderBy = null,
            bool isDescending = false,
            CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes);

        Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);

        Task AddAsync(T entity, CancellationToken cancellationToken = default);

        void Update(T entity);

        IQueryable<T> GetQueryable();

        Task<int> TotalAsync(Expression<Func<T, bool>> predicate);
    }
}
