using System;

namespace Application.DTOs.Delivery
{
    public record CreateShipmentRequest(Guid OrderId, string Carrier = "GHN");
}
