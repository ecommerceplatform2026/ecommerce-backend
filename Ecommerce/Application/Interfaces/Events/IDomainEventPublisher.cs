using Domain.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces.Events
{
    public interface IDomainEventPublisher
    {
        Task PublishAsync(IEvent domainEvent, CancellationToken cancellationToken = default);
    }
}
