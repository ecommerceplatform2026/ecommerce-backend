using Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Events
{
    public interface IIntegrationEventPublisher
    {
        Task PublishAsync(IEvent integrationEvent, CancellationToken cancellationToken = default);
    }
}
