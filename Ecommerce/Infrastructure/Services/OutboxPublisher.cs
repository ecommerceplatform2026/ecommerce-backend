using Application.Interfaces.Events;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class OutboxPublisher : IIntegrationEventPublisher
    {
        private readonly EcommerceContext _context;

        public OutboxPublisher(EcommerceContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public Task PublishAsync(IEvent @event, CancellationToken cancellationToken = default)
        {
            if (@event is not IIntegrationEvent) return Task.CompletedTask;

            var eventType = @event.GetType().AssemblyQualifiedName!;
            var json = JsonSerializer.Serialize(@event, @event.GetType());
            _context.OutboxMessages.Add(new OutboxMessage(eventType, json));

            return Task.CompletedTask;
        }
    }
}
