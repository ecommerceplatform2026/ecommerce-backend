using Application.Common.Caching;
using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Events
{
    public class InvalidateProductCacheHandler : 
        IDomainEventHandler<PaymentConfirmedDomainEvent>,
        IDomainEventHandler<ProductStockUpdatedDomainEvent>
    {
        private readonly ICacheService _cacheService;

        public InvalidateProductCacheHandler(ICacheService cacheService)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public async Task HandleAsync(PaymentConfirmedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            try
            {
                await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);

                var productIds = domainEvent.Order.OrderItems
                    .Select(oi => oi.ProductVariant?.ProductId)
                    .Where(id => id.HasValue && id.Value != Guid.Empty)
                    .Select(id => id!.Value)
                    .Distinct()
                    .ToList();

                foreach (var productId in productIds)
                {
                    await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(productId), cancellationToken);
                }
            }
            catch
            {
                // Cache invalidation is best-effort
            }
        }

        public async Task HandleAsync(ProductStockUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            try
            {
                await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
                await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(domainEvent.ProductId), cancellationToken);
            }
            catch
            {
                // Cache invalidation is best-effort
            }
        }
    }
}
