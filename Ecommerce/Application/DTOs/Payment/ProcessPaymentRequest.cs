using Application.Common.Validations;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Payment
{
    public sealed record ProcessPaymentRequest(
        [Required(ErrorMessage = "PaymentLinkId is required.")]
        [NotWhiteSpace(ErrorMessage = "PaymentLinkId must not be empty or whitespace.")]
        string PaymentLinkId,
        
        bool IsSuccess);
}
