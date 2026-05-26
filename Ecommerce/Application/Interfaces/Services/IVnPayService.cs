using System.Collections.Generic;

namespace Application.Interfaces.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(int orderCode, long totalAmount);
        bool ValidateCallback(IDictionary<string, string> queryParameters, out int orderCode, out bool isSuccess);
    }
}
