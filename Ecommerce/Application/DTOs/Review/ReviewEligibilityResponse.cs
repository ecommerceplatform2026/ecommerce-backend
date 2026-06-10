using System;
using System.Collections.Generic;

namespace Application.DTOs.Review
{
    public sealed record EligibleOrderDto(
        Guid OrderId,
        int OrderCode,
        DateTime CreatedAt,
        string Status);

    public sealed record ReviewEligibilityResponse(
        bool IsEligible,
        List<EligibleOrderDto> EligibleOrders);
}
