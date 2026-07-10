using System;

namespace Domain.Common
{
    public interface IEvent
    {
        DateTime OccurredOn { get; }
    }

    public interface IDomainEvent : IEvent { }

    public interface IIntegrationEvent : IEvent { }
}
