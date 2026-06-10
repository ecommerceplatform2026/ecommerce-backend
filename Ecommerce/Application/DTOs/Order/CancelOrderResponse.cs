using System;

namespace Application.DTOs.Order
{
    public sealed record CancelOrderResponse(
        Guid OrderId,
        int OrderCode,
        string Status,
        int? RefundedPoints,
        string? RefundDescription);
}
