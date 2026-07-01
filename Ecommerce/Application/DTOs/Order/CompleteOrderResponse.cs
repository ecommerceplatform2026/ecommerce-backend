using System;

namespace Application.DTOs.Order
{
    public sealed record CompleteOrderResponse(
        Guid OrderId,
        int OrderCode,
        string Status);
}
