using Application.Interfaces.Events;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class OutboxPublisher : IIntegrationEventPublisher
    {
        private readonly EcommerceContext _context;
        private readonly IntegrationHandlerRegistry _registry;
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        public OutboxPublisher(EcommerceContext context, IntegrationHandlerRegistry registry)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event is not IIntegrationEvent) return Task.CompletedTask;

            var eventType = @event.GetType().AssemblyQualifiedName!;
            var json = JsonConvert.SerializeObject(@event, @event.GetType(), JsonSettings);
            var handlers = _registry.GetHandlerTypes(@event.GetType());

            foreach (var handlerType in handlers)
            {
                _context.OutboxMessages.Add(new OutboxMessage(eventType, json, handlerType.AssemblyQualifiedName!));
            }

            return Task.CompletedTask;
        }
    }
}
