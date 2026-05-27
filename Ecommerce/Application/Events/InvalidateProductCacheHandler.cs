using Application.Common.Caching;
using Application.Interfaces.Events;
using Application.Interfaces.Services;
using Domain.Events;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<InvalidateProductCacheHandler> _logger;

        public InvalidateProductCacheHandler(ICacheService cacheService, ILogger<InvalidateProductCacheHandler> logger)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate product cache for payment confirmed event (OrderId: {OrderId}).", domainEvent.Order.Id);
            }
        }

        public async Task HandleAsync(ProductStockUpdatedDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            try
            {
                await _cacheService.RemoveAsync(CacheKeys.ProductsAll, cancellationToken);
                await _cacheService.RemoveAsync(CacheKeys.GetProductDetailKey(domainEvent.ProductId), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate product cache for stock updated event (ProductId: {ProductId}).", domainEvent.ProductId);
            }
        }
    }
}
