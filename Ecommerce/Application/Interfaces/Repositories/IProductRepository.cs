using Application.Common.Response;
using Application.DTOs.Product;
using Domain.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Repositories
{
    public interface IProductRepository
    {
        Task<PagedResult<Product>> GetProductsAsync(ProductListingRequest request, CancellationToken cancellationToken = default);
    }
}
