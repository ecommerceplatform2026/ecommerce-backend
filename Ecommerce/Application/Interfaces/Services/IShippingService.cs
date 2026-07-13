using Application.Common.Response;
using Application.DTOs.Delivery;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    /// <summary>
    /// Carrier-agnostic shipping orchestrator.
    /// Resolves carrier provider by carrier code and delegates to it.
    /// </summary>
    public interface IShippingService
    {
        /// <summary>
        /// Create shipment for given order using specified carrier.
        /// </summary>
        Task<Result<ShipmentResponse>> CreateShipmentAsync(
            CreateShipmentRequest request,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retry a failed delivery that is in Exception status.
        /// </summary>
        Task<Result<ShipmentResponse>> RetryShipmentAsync(
            Guid deliveryId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// List deliveries with optional filters and paging.
        /// </summary>
        Task<Result<PagedResult<ShipmentDetailResponse>>> GetDeliveriesAsync(
            GetShipmentRequest request,
            CancellationToken cancellationToken = default);
    }
}
