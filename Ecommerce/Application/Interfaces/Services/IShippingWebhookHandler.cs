using Application.Common.Response;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IShippingWebhookHandler
    {
        string CarrierCode { get; }
        Task<Result<string>> ProcessStatusUpdateAsync(object payload, CancellationToken cancellationToken = default);
    }
}
