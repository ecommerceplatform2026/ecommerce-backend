using Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Events
{
    public interface IIntegrationEventHandler<in TEvent> where TEvent : IIntegrationEvent
    {
        Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
    }
}
