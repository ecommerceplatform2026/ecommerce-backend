using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Checkout
{
    public sealed record CheckoutRequest(
        [Required(ErrorMessage = "PaymentMethod is required.")]
        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "Invalid PaymentMethod.")]
        PaymentMethod PaymentMethod);
}
