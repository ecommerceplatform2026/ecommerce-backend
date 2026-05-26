using Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Events
{
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    }
}
