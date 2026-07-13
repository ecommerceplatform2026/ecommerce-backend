using Application.Interfaces.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Infrastructure.Services
{
    public sealed class IntegrationHandlerRegistry
    {
        private readonly Dictionary<Type, List<Type>> _handlerMap;

        public IntegrationHandlerRegistry(Assembly handlersAssembly)
        {
            _handlerMap = handlersAssembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .SelectMany(t => t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IIntegrationEventHandler<>))
                    .Select(i => new { EventType = i.GetGenericArguments()[0], Handler = t }))
                .GroupBy(x => x.EventType)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Handler).ToList());
        }

        public IReadOnlyList<Type> GetHandlerTypes(Type eventType)
            => _handlerMap.TryGetValue(eventType, out var handlers)
                ? handlers
                : Array.Empty<Type>();
    }
}
