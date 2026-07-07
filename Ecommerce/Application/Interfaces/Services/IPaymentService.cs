using Application.Common.Response;
using Application.DTOs.Payment;

namespace Application.Interfaces.Services
{
    public interface IPaymentService
    {
        // ponytail: old return-url callbacks, kept for reference
        // Task<Result<PaymentResponse>> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);
        // Task<Result<PaymentResponse>> ProcessMomoCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);
        // Task<Result<PaymentResponse>> ProcessZaloPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);

        Task<Result<PaymentResponse>> ProcessVnPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);
        Task<Result<PaymentResponse>> ProcessMomoCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);
        Task<Result<PaymentResponse>> ProcessZaloPayCallbackAsync(IDictionary<string, string> queryParameters, CancellationToken cancellationToken = default);
        Task<Result<PaymentResponse>> GetPaymentStatusAsync(int orderCode, CancellationToken cancellationToken = default);
    }
}
