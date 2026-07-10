using Application.Interfaces.Events;
using Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class DomainEventPublisher : IDomainEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;

        public DomainEventPublisher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public async Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event is not IDomainEvent) return;

            var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(@event.GetType());
            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler != null)
                {
                    var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync));
                    if (method != null)
                    {
                        var task = (Task)method.Invoke(handler, new object[] { @event, cancellationToken })!;
                        await task;
                    }
                }
            }
        }
    }
}
