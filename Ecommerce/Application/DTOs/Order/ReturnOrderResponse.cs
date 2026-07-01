using System;

namespace Application.DTOs.Order
{
    public sealed record ReturnOrderResponse(
        Guid OrderId,
        int OrderCode,
        string Status,
        DateTime? ReturnByDate);
}
