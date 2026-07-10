using System;
using Domain.Helpers;

namespace Domain.Common
{
    public abstract class BaseEntity
    {
        public Guid Id { get; protected set; } = Guid.NewGuid();
        public DateTime CreatedAt { get; protected set; }
        public string? CreatedBy { get; protected set; }
        public DateTime? UpdatedAt { get; protected set; }
        public string? UpdatedBy { get; protected set; }
        public bool IsDeleted { get; protected set; }
        public DateTime? DeletedAt { get; protected set; }
        public string? DeletedBy { get; protected set; }

        public void SetCreated(string userId)
        {
            CreatedAt = TimeHelper.GetTime();
            CreatedBy = userId;
        }

        public void SetUpdated(string userId)
        {
            UpdatedAt = TimeHelper.GetTime();
            UpdatedBy = userId;
        }

        public void SetDeleted(string userId)
        {
            IsDeleted = true;
            DeletedAt = TimeHelper.GetTime();
            DeletedBy = userId;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
            DeletedBy = null;
        }

        private readonly List<IEvent> _domainEvents = new();
        public IReadOnlyCollection<IEvent> DomainEvents => _domainEvents.AsReadOnly();

        protected void AddDomainEvent(IEvent @event)
        {
            _domainEvents.Add(@event);
        }

        public void ClearDomainEvents()
        {
            _domainEvents.Clear();
        }
    }
}
