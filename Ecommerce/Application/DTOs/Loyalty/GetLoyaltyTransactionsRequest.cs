using Application.Common.Response;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Loyalty
{
    public sealed class GetLoyaltyTransactionsRequest : PagingRequest
    {
        [EnumDataType(typeof(LoyaltyTransactionType), ErrorMessage = "Invalid LoyaltyTransactionType.")]
        public LoyaltyTransactionType? Type { get; set; }

        [EnumDataType(typeof(LoyaltyTransactionStatus), ErrorMessage = "Invalid LoyaltyTransactionStatus.")]
        public LoyaltyTransactionStatus? Status { get; set; }
    }
}
