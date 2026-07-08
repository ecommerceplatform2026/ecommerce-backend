using System;

namespace Application.DTOs.Delivery
{
    public record RetryShipmentRequest(Guid DeliveryId);
}
