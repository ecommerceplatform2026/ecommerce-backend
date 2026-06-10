using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Loyalty
{
    public sealed record RedeemPointsRequest(
        [NotEmptyGuid(ErrorMessage = "Order ID cannot be empty.")] Guid OrderId,
        [Range(1, int.MaxValue, ErrorMessage = "Points must be greater than zero.")]
        int Points);
}
