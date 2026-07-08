using Application.Interfaces.Repositories.Base;
using Application.Interfaces.Events;
using Application.Common.Exceptions;
using Domain.Common;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Repositories.Base
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EcommerceContext _context;
        private readonly IDomainEventPublisher _publisher;
        private readonly Dictionary<Type, object> _repositories = new();
        private readonly List<IDomainEvent> _pendingDomainEvents = new();

        public UnitOfWork(EcommerceContext context, IDomainEventPublisher publisher)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        }
        public IGenericRepository<T> GetRepository<T>() where T : BaseEntity
        {
            var type = typeof(T);

            if (!_repositories.ContainsKey(type))
            {
                var repoInstance = new GenericRepository<T>(_context);
                _repositories[type] = repoInstance;
            }

            return (IGenericRepository<T>)_repositories[type];
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var domainEntities = _context.ChangeTracker
                .Entries<BaseEntity>()
                .Where(x => x.Entity.DomainEvents != null && x.Entity.DomainEvents.Any())
                .ToList();

            var domainEvents = domainEntities
                .SelectMany(x => x.Entity.DomainEvents)
                .ToList();

            int result;
            try
            {
                result = await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                const int maxEntriesToShow = 5;
                var entries = ex.Entries.Select(e => $"{e.Entity.GetType().Name} (State: {e.State})").ToList();
                var entryDetails = string.Join("; ", entries.Take(maxEntriesToShow));
                var moreSuffix = entries.Count > maxEntriesToShow ? $" (+{entries.Count - maxEntriesToShow} more)" : string.Empty;
                throw new ConcurrencyException($"A concurrency conflict occurred while saving changes. Entities involved: {entryDetails}{moreSuffix}", ex);
            }

            if (HasActiveTransaction)
            {
                _pendingDomainEvents.AddRange(domainEvents);

                foreach (var entity in domainEntities)
                {
                    entity.Entity.ClearDomainEvents();
                }
            }
            else
            {
                foreach (var domainEvent in domainEvents)
                {
                    await _publisher.PublishAsync(domainEvent, cancellationToken);
                }

                foreach (var entity in domainEntities)
                {
                    entity.Entity.ClearDomainEvents();
                }
            }

            return result;
        }

        private async Task PublishPendingDomainEventsAsync(CancellationToken cancellationToken = default)
        {
            foreach (var domainEvent in _pendingDomainEvents)
            {
                await _publisher.PublishAsync(domainEvent, cancellationToken);
            }
            _pendingDomainEvents.Clear();
        }

        public bool HasActiveTransaction => _context.Database.CurrentTransaction != null;

        public async Task<ITransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new EfTransaction(
                transaction,
                onCommit: ct => PublishPendingDomainEventsAsync(ct),
                onRollback: ct => { _pendingDomainEvents.Clear(); return Task.CompletedTask; });
        }
        
        public void ClearTracker()
        {
            _context.ChangeTracker.Clear();
        }

        public Application.Interfaces.Repositories.Base.IExecutionStrategy CreateExecutionStrategy()
        {
            return new EfExecutionStrategy(_context.Database.CreateExecutionStrategy());
        }
    }
}
