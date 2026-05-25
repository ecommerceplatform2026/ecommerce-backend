using Application.Common.Response;
using Application.DTOs.Checkout;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface ICheckoutService
    {
        Task<Result<CheckoutResponse>> ProcessCheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
    }
}
