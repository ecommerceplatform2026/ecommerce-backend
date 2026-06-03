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
        /// Validates address, calls carrier API, saves tracking,
        /// transitions order to Shipping.
        /// Returns full ShipmentResponse on success.
        /// </summary>
        Task<Result<ShipmentResponse>> CreateShipmentAsync(
            Guid orderId,
            string carrierCode,
            CancellationToken cancellationToken = default);
    }
}
