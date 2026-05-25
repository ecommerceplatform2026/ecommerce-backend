using Application.Common.Response;
using Application.DTOs.Payment;

namespace Application.Interfaces.Services
{
    public interface IPaymentService
    {
        Task<Result<PaymentResponse>> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);
    }
}
