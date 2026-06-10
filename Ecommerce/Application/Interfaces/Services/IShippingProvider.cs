using Application.Common.Response;
using Application.DTOs.Delivery;
using Application.DTOs.Delivery.GHN;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    /// <summary>
    /// Strategy interface. One implementation per carrier (GHN, GHTK, etc.).
    /// Each provider handles its own API auth, payload format, address validation,
    /// and response parsing internally.
    /// </summary>
    public interface IShippingProvider
    {
        string CarrierCode { get; }

        Task<Result<ShipmentResponse>> CreateShipmentAsync(
            Guid orderId,
            CreateGhnShipmentRequest shipmentInfo,
            CancellationToken cancellationToken = default);
    }
}
