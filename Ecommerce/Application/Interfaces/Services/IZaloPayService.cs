using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IZaloPayService
    {
        Task<string> CreatePaymentUrlAsync(int orderCode, long totalAmount, CancellationToken cancellationToken = default);
        bool ValidateCallback(IDictionary<string, string> parameters, out int orderCode, out bool isSuccess);
    }
}
